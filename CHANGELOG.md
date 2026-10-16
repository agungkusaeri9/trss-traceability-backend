# Catatan Rilis (Changelog) - TRSS Backend

Dokumen ini mencatat ringkasan perubahan dan pembaruan sistem pada Backend TRSS Traceability berdasarkan versi rilis.

---

## [v0.2.3] - 16 Oktober 2026

### Perbaikan & Penyesuaian
- **Penyesuaian Posisi Teks Label Clinching (ZPL)**:
  - Menggeser posisi koordinat Y teks `mitsubishiCode` (dari 16 ke 18) dan `trssCode` (dari 42 ke 45) pada template label besar ZPL agar tata letak teks lebih presisi dan rapi.

---

## [v0.2.2] - 16 Oktober 2026

### Perbaikan & Penyesuaian

- **Pemisahan Kode TRSS Label Clinching & M-Fan Assy**:
  - Mengubah kode TRSS pada format pencetakan label QR **Clinching** menjadi **`BM57100000`**.
  - Kode TRSS pada label **M-Fan Assy** tetap menggunakan **`BM57200000`**, sehingga kini kedua proses memiliki kode identifikasi label yang berbeda dan sesuai dengan spesifikasi masing-masing.
- **Kolom PartNumber & PartNumberCustomer di Master Process Categories**:
  - Menambahkan kolom `part_number` dan `part_number_customer` pada entitas & tabel `process_categories` beserta file migrasi EF Core.
  - Nilai default diatur:
    - Kategori **`clinching`**: `part_number` = **`BM57100000`**, `part_number_customer` = **`21400C000P`**.
    - Kategori **`mfan`**: `part_number` = **`BM57200000`**, `part_number_customer` = **`21580C000P`**.
- **Dinamisir Kode Label di PrintService**:
  - Fungsi `PrintClinchingLabelWithSdkAsync` dan `PrintMFanAssyLabelWithSdkAsync` kini membaca `mitsubishiCode` dari kolom `part_number_customer` dan `trssCode` dari kolom `part_number` di tabel `process_categories` secara dinamis dari database (dengan fallback nilai default).
- **Payload MQTT Scan Clinching & M-Fan Assy**:
  - Menambahkan kolom `part_number` (sejajar dengan `is_printed`) pada payload MQTT topik `data/process/clinching-short-side/process-scan` dan `data/process/m-fan-assy/process-scan` di `ProcessLogService` yang diambil dari tabel `process_categories` secara dinamis.

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
  - Menambahkan pencatatan riwayat (_history_) untuk setiap perubahan nilai parameter proses, termasuk pada proses ECM dan Final Inspection.
  - Menampilkan status pengiriman data MQTT per proses (apakah aktif atau dinonaktifkan beserta alasannya).
  - Menambahkan fitur pencarian daftar nomor lot/issue berdasarkan nomor seri produk.
- **Standarisasi Jam & Waktu**:
  - Menyeragamkan seluruh data waktu pembuatan dan pembaruan (`CreatedAt` / `UpdatedAt`) menggunakan standar UTC. Jam di tampilan web/frontend tidak akan lagi bergeser atau bertambah 7 jam secara keliru.
- **Keandalan Cetak Label (Printer Zebra)**:
  - Menambahkan batas waktu tunggu (_timeout_) 3 detik saat menghubungi printer. Jika printer mati atau jaringan terputus, sistem langsung merespons dan tidak menyebabkan aplikasi macet (_hang_).
- **Pengelolaan Stok & Nomor Seri**:
  - Sisa stok part (_remaining quantity_) kini otomatis dihitung dan dipotong sesuai jumlah pasokan (_supply_).
  - Penomoran seri produk kini otomatis menyesuaikan pergantian tahun (_annual reset_).
- **Pembersihan Data Duplikat**:
  - Jika data hasil scan masuk berulang kali, sistem akan langsung memperbarui data yang ada di tempat, bukan membuat data duplikat baru.
  - Data hasil scan biasa tidak lagi dimasukkan sembarangan ke dalam tabel riwayat (_history_).

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
  - Fitur cetak ulang (_reprint_) jika label rusak atau hilang.
- **Penerimaan Barang & Rework**:
  - Modul input stok masuk (_Stock In_) dengan generate label QR khusus.
  - Modul pencatatan part yang perlu diperbaiki (_Stock In Rework_).
- **Data Master & Keamanan**:
  - Pengelolaan data part komponen, daftar proses kerja, dan parameter batas ukur.
  - Manajemen akun pengguna dan hak akses berbasis peran (_role-based access_).
  - Sistem pencadangan (_backup_) database otomatis secara berkala.
