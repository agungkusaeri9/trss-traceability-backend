# Dokumentasi WebSocket (SignalR)

Dokumentasi ini menjelaskan cara client (Frontend/Dashboard) terhubung ke WebSocket untuk memantau status printer secara real-time.

## 1. Endpoint Hub
- **URL**: `http://localhost:5039/hubs/printer`
- **Protocol**: SignalR (WebSockets / Server-Sent Events / Long Polling)

## 2. Event: `PrinterStatusUpdated`
Sistem akan mengirimkan data secara otomatis (broadcast) setiap **10 detik** hanya jika ditemukan printer yang sedang **Offline**.

### Data Payload:
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

## 3. Contoh Implementasi (JavaScript)

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

## 4. Catatan Penting
- **Auto-Update DB**: Background service di backend secara otomatis akan merubah kolom `IsActive` di database menjadi `false` jika printer terdeteksi offline, dan kembali ke `true` jika online.
- **Noise Reduction**: Jika semua printer dalam kondisi **Online**, backend tidak akan mengirimkan broadcast apapun untuk menghemat resource jaringan.
