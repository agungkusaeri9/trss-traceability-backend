# Dokumentasi MQTT

Dokumentasi ini menjelaskan cara menggunakan MQTT untuk berkomunikasi dengan sistem Traceability, terutama untuk permintaan print.

## 1. Konfigurasi Koneksi MQTT

Backend terhubung ke broker MQTT dengan konfigurasi yang bisa diatur di file `appsettings.json`:

```json
{
  "MqttSettings": {
    "Broker": "localhost",
    "Port": 1883,
    "ClientId": "trss-traceability-backend"
  }
}
```

### Detail Konfigurasi:
| Parameter | Tipe | Deskripsi | Default |
|-----------|------|-----------|---------|
| Broker | string | Alamat IP/domain broker MQTT | localhost |
| Port | number | Port broker MQTT | 1883 |
| ClientId | string | ID client untuk backend | trss-traceability-backend |

## 2. Topic MQTT

Sistem menggunakan **2 topic** untuk menerima permintaan print:

| Topic | Kode Proses |
|-------|-------------|
| `traceability/print/request/clinching-short-side` | `CLINCHING_SHORT_SIDE` |
| `traceability/print/request/m-fan-assy` | `M_FAN_ASSY` |

## 3. Format Payload MQTT

Payload harus berupa JSON dengan struktur berikut:

```json
{
  "issue_number": "ISS-001"
}
```

### Detail Field Payload:
| Field | Tipe | Deskripsi | Contoh |
|-------|------|-----------|--------|
| issue_number | string | Nomor issue | "ISS-001" |

### Contoh Payload yang Valid:

#### Contoh 1: Proses M_FAN_ASSY
Kirim ke topic: `traceability/print/request/m-fan-assy`
```json
{
  "issue_number": "ISS-001"
}
```

#### Contoh 2: Proses CLINCHING_SHORT_SIDE
Kirim ke topic: `traceability/print/request/clinching-short-side`
```json
{
  "issue_number": "ISS-001"
}
```

## 4. Cara Mengirim Pesan MQTT (Contoh)

### Contoh dengan Mosquitto CLI:
Untuk M_FAN_ASSY:
```bash
mosquitto_pub -h localhost -p 1883 -t "traceability/print/request/m-fan-assy" -m "{\"issue_number\":\"ISS-001\"}"
```

Untuk CLINCHING_SHORT_SIDE:
```bash
mosquitto_pub -h localhost -p 1883 -t "traceability/print/request/clinching-short-side" -m "{\"issue_number\":\"ISS-001\"}"
```

### Contoh dengan Python (paho-mqtt):
```python
import paho.mqtt.client as mqtt
import json

# Konfigurasi
broker = "localhost"
port = 1883
topic_clinching = "traceability/print/request/clinching-short-side"
topic_mfanassy = "traceability/print/request/m-fan-assy"

# Data payload
payload = {
    "issue_number": "ISS-001"
}

# Buat client
client = mqtt.Client()

# Koneksi ke broker
client.connect(broker, port)

# Kirim pesan untuk CLINCHING_SHORT_SIDE
client.publish(topic_clinching, json.dumps(payload))

# Atau kirim pesan untuk M_FAN_ASSY
# client.publish(topic_mfanassy, json.dumps(payload))

# Putuskan koneksi
client.disconnect()
```

## 5. Penyimpanan ke Database

Untuk menjaga keandalan (reliability), setiap pesan MQTT yang diterima akan **disimpan ke database terlebih dahulu** sebelum diproses. Ini bertujuan untuk menghindari kehilangan pesan jika terjadi error saat proses print.

### 5.1 Struktur Tabel `mqtt_print_requests`

| Field | Tipe | Deskripsi |
|-------|------|-----------|
| id | bigint | Primary key |
| process_code | varchar | Kode proses (dari topic) |
| issue_number | varchar | Nomor issue (dari payload) |
| raw_payload | text | Payload JSON mentah yang diterima |
| status | varchar | Status pesan ("Pending", "Processed", "Failed") |
| error_message | text | Pesan error jika terjadi kesalahan |
| created_at | datetime | Waktu pesan diterima |
| processed_at | datetime | Waktu pesan diproses (jika berhasil) |
| updated_at | datetime | Waktu terakhir diperbarui |

### 5.2 Alur Kerja

1. Backend menerima pesan MQTT
2. Simpan pesan ke database dengan status `Pending`
3. Lanjutkan proses print
4. Jika berhasil: update status jadi `Processed` dan isi `processed_at`
5. Jika gagal: update status jadi `Failed` dan isi `error_message`

### 5.3 Catatan Penting

- Semua pesan (termasuk yang gagal diproses) akan tetap tersimpan di database untuk audit dan troubleshooting
- Kamu bisa menambahkan logic untuk memproses ulang pesan yang statusnya `Failed` nanti

## 6. Logging di Backend

Setiap pesan MQTT yang diterima oleh backend akan dicatat di log (Serilog) dengan format:
```
[HH:MM:SS INF] Received MQTT message on topic traceability/print/request/m-fan-assy: {"issue_number":"ISS-001"}
[HH:MM:SS INF] MQTT message saved to DB with ID: 1
```
