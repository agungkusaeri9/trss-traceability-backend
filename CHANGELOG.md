# Catatan Rilis (Changelog) - TRSS Backend

Dokumen ini mencatat ringkasan perubahan dan pembaruan sistem pada Backend TRSS Traceability berdasarkan versi rilis.

---

## [v0.2.1] - 16 Oktober 2026

### Pembaruan
- **Format Hasil Pengecekan (OK / NG)**:
  - Nilai hasil pengecekan yang sebelumnya muncul sebagai angka `1` atau `2` kini otomatis diubah menjadi teks **`OK`** atau **`NG`** agar mudah dibaca pada riwayat dan detail proses log.

---

## [v0.2.0] - 16 Oktober 2026

### Fitur Baru & Peningkatan
- **Tampilan & API Traceability Versi 2**:
  - Pengelompokan proses produksi dibuat lebih rapi dan terpisah jelas (Clinching, HE Leak, M-Fan, ECM Assy, dan Final Inspection).
  - Pengecekan multi-titik (seperti tinggi Clinching dan lebar End Plate) otomatis dirangkum menjadi satu baris agar tidak memenuhi tabel.
  - Menambahkan pencatatan riwayat (*history*) untuk setiap perubahan nilai parameter proses, termasuk pada proses ECM dan Final Inspection.
  - Menampilkan status pengiriman data MQTT per proses (apakah aktif atau dinonaktifkan beserta alasannya).
  - Menambahkan fitur pencarian daftar nomor lot/issue berdasarkan nomor seri produk.
- **Standarisasi Jam & Waktu**:
  - Menyeragamkan seluruh data waktu pembuatan dan pembaruan (`CreatedAt` / `UpdatedAt`) menggunakan standar UTC. Jam di tampilan web/frontend tidak akan lagi bergeser atau bertambah 7 jam secara keliru.
- **Keandalan Cetak Label (Printer Zebra)**:
  - Menambahkan batas waktu tunggu (*timeout*) 3 detik saat menghubungi printer. Jika printer mati atau jaringan terputus, sistem langsung merespons dan tidak menyebabkan aplikasi macet (*hang*).
- **Pengelolaan Stok & Nomor Seri**:
  - Sisa stok part (*remaining quantity*) kini otomatis dihitung dan dipotong sesuai jumlah pasokan (*supply*).
  - Penomoran seri produk kini otomatis menyesuaikan pergantian tahun (*annual reset*).
- **Pembersihan Data Duplikat**:
  - Jika data hasil scan masuk berulang kali, sistem akan langsung memperbarui data yang ada di tempat, bukan membuat data duplikat baru.
  - Data hasil scan biasa tidak lagi dimasukkan sembarangan ke dalam tabel riwayat (*history*).

### Keamanan Sistem
- Memperbarui komponen paket internal sistem ke versi yang lebih aman dari celah keamanan.
- Membersihkan pesan peringatan audit sistem agar proses pemantauan aplikasi lebih bersih.

---

## [v0.1.0] - 04 September 2026

### Peluncuran Awal Sistem (Fitur Inti)
- **Pencatatan Proses Produksi (Traceability Core)**:
  - Pencatatan alur produksi mulai dari sub-assy hingga perakitan utama.
  - Fitur ekspor laporan riwayat proses ke format Excel.
- **Komunikasi Mesin & PLC (MQTT Worker)**:
  - Layanan latar belakang untuk menerima data hasil pengukuran mesin secara otomatis via MQTT.
  - Pemicu otomatis untuk mencetak label setelah proses perakitan selesai.
- **Pencetakan Barcode & QR Label**:
  - Integrasi langsung dengan printer industri Zebra melalui jaringan LAN/IP.
  - Fitur cetak ulang (*reprint*) jika label rusak atau hilang.
- **Penerimaan Barang & Rework**:
  - Modul input stok masuk (*Stock In*) dengan generate label QR khusus.
  - Modul pencatatan part yang perlu diperbaiki (*Stock In Rework*).
- **Data Master & Keamanan**:
  - Pengelolaan data part komponen, daftar proses kerja, dan parameter batas ukur.
  - Manajemen akun pengguna dan hak akses berbasis peran (*role-based access*).
  - Sistem pencadangan (*backup*) database otomatis secara berkala.
