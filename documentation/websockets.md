# Dokumentasi WebSocket (SignalR)

Dokumentasi ini menjelaskan cara client (Frontend/Dashboard) terhubung ke WebSocket untuk memantau status printer dan status koneksi MQTT secara real-time.

---

## 1. Printer Monitor Hub

### 1.1 Endpoint Hub

- **URL**: `http://localhost:5039/hubs/printer`
- **Protocol**: SignalR (WebSockets / Server-Sent Events / Long Polling)

### 1.2 Event: `PrinterStatusUpdated`

Sistem akan mengirimkan data secara otomatis (broadcast) setiap **10 detik** hanya jika ditemukan printer yang sedang **Offline**.

#### Data Payload:

Payload berupa **Array of Objects**:

```json
[
  {
    "id": 1,
    "name": "Printer Thermal 01",
    "ipAddress": "192.168.1.100",
    "port": 9100,
    "isOnline": false,
    "status": "Offline",
    "lastChecked": "2026-05-08T16:30:00.123Z"
  }
]
```

### 1.3 Contoh Implementasi (JavaScript)

```javascript
const signalR = require("@microsoft/signalr");

// 1. Buat koneksi
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5039/hubs/printer")
  .withAutomaticReconnect()
  .build();

// 2. Daftarkan Listener
connection.on("PrinterStatusUpdated", (offlinePrinters) => {
  console.log("Daftar Printer Offline:", offlinePrinters);

  if (offlinePrinters.length > 0) {
    // Tampilkan alert atau notifikasi di UI
    alert(`${offlinePrinters.length} Printer sedang Offline!`);
  }
});

// 3. Start koneksi
async function start() {
  try {
    await connection.start();
    console.log("SignalR Connected.");
  } catch (err) {
    console.log(err);
    setTimeout(start, 5000);
  }
}

start();
```

### 1.4 Catatan Penting

- **Auto-Update DB**: Background service di backend secara otomatis akan merubah kolom `IsActive` di database menjadi `false` jika printer terdeteksi offline, dan kembali ke `true` jika online.
- **Noise Reduction**: Jika semua printer dalam kondisi **Online**, backend tidak akan mengirimkan broadcast apapun untuk menghemat resource jaringan.

---

## 2. MQTT Status Hub

### 2.1 Endpoint Hub

- **URL**: `http://localhost:5039/hubs/mqtt-status`
- **Protocol**: SignalR (WebSockets / Server-Sent Events / Long Polling)

### 2.2 Event: `MqttStatusUpdated`

Sistem akan mengirimkan data secara otomatis (broadcast) setiap kali status koneksi MQTT berubah (Online/Offline). Selain itu, klien yang baru terhubung akan langsung menerima status saat ini.

#### Data Payload:

Payload berupa **Object**:

```json
{
  "isConnected": true,
  "broker": "localhost",
  "port": 1883,
  "status": "Online"
}
```

#### Detail Field Payload:

| Field       | Tipe    | Deskripsi                                            | Contoh      |
| ----------- | ------- | ---------------------------------------------------- | ----------- |
| isConnected | boolean | Status koneksi MQTT (true = Online, false = Offline) | true        |
| broker      | string  | Alamat broker MQTT                                   | "localhost" |
| port        | number  | Port broker MQTT                                     | 1883        |
| status      | string  | Status koneksi dalam teks ("Online" atau "Offline")  | "Online"    |

### 2.3 Contoh Implementasi (JavaScript)

```javascript
const signalR = require("@microsoft/signalr");

// 1. Buat koneksi
const mqttConnection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5039/hubs/mqtt-status")
  .withAutomaticReconnect()
  .build();

// 2. Daftarkan Listener
mqttConnection.on("MqttStatusUpdated", (mqttStatus) => {
  console.log("Status MQTT:", mqttStatus);

  // Update UI sesuai status
  const statusElement = document.getElementById("mqtt-status");
  statusElement.textContent = mqttStatus.status;
  statusElement.className = mqttStatus.isConnected
    ? "text-green-500"
    : "text-red-500";
});

// 3. Start koneksi
async function startMqttConnection() {
  try {
    await mqttConnection.start();
    console.log("MQTT Status Hub Connected.");
  } catch (err) {
    console.log(err);
    setTimeout(startMqttConnection, 5000);
  }
}

startMqttConnection();
```

### 2.4 Catatan Penting

- **Auto-Reconnect**: Jika koneksi ke broker MQTT putus, backend akan otomatis mencoba reconnect setiap **5 detik**.
- **Realtime Update**: Setiap perubahan status koneksi MQTT akan langsung dikirim ke semua klien yang terhubung.

---

## 3. Traceability Summary Hub

### 3.1 Endpoint Hub

- **URL**: `http://localhost:5039/hubs/traceability-summary`
- **Protocol**: SignalR (WebSockets / Server-Sent Events / Long Polling)

### 3.2 Event: `TraceabilitySummaryUpdated`

Sistem mengirim data summary traceability ke client. Client yang baru connect langsung menerima snapshot terbaru. Semua client yang terhubung menerima broadcast setiap kali ada data baru (misalnya setelah process log / MQTT event di backend).

#### Data Payload:

Payload berupa **Array of Objects** (sama dengan `GET /api/dashboard/traceability-summary`):

```json
[
  {
    "order": 1,
    "name": "nameLabelSerialNo",
    "label": "Name Label Serial No",
    "value": "SN-202604220001"
  },
  {
    "order": 25,
    "name": "finalInspection",
    "label": "Final Inspection",
    "value": "PASS"
  }
]
```

### 3.3 Contoh Implementasi (JavaScript)

```javascript
const signalR = require("@microsoft/signalr");

const traceabilityConnection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5039/hubs/traceability-summary")
  .withAutomaticReconnect()
  .build();

traceabilityConnection.on("TraceabilitySummaryUpdated", (summary) => {
  console.log("Traceability summary:", summary);

  summary.forEach((field) => {
    console.log(`${field.order}. ${field.label}:`, field.value);
  });
});

async function startTraceabilityConnection() {
  try {
    await traceabilityConnection.start();
    console.log("Traceability Summary Hub Connected.");
  } catch (err) {
    console.log(err);
    setTimeout(startTraceabilityConnection, 5000);
  }
}

startTraceabilityConnection();
```

### 3.4 Catatan Penting

- **Initial snapshot**: Saat client connect, backend langsung mengirim event `TraceabilitySummaryUpdated` dengan data terbaru.
- **Broadcast update**: Backend memanggil `ITraceabilitySummaryNotifier.BroadcastAsync()` setiap kali data traceability berubah (contoh: setelah process log baru masuk).
- **REST fallback**: Endpoint `GET /api/dashboard/traceability-summary` tetap tersedia untuk fetch manual / initial load tanpa WebSocket.
