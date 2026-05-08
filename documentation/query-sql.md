# Documentation: Stored Procedure for Traceability Log

Dokumentasi ini menjelaskan penggunaan Stored Procedure `sp_insert_trace_data` untuk mencatat data traceability dari sistem eksternal (HMI/IoT).

## 1. Deskripsi

Stored Procedure ini digunakan untuk memasukkan data log produksi (`process_logs`) beserta rincian parameter-parameternya (`process_log_details`) secara atomik dalam satu transaksi. Prosedur ini mendukung pencarian ID secara otomatis berdasarkan **Process Code** dan **Parameter Code**.

## 2. Definisi Prosedur

### Input Parameters:

| Parameter        | Tipe Data      | Deskripsi                                                                                  |
| :--------------- | :------------- | :----------------------------------------------------------------------------------------- |
| `p_issue_no`     | `VARCHAR(100)` | Nomor Issue/Serial Number produk.                                                          |
| `p_is_active`    | `BOOLEAN`      | Status hasil (1 = OK, 0 = NG). Jika 0, sistem otomatis menambah suffix `-R` pada issue_no. |
| `p_process_code` | `VARCHAR(50)`  | Kode Proses (misal: `PRC-005`).                                                            |
| `p_details_json` | `JSON`         | Array JSON berisi kumpulan parameter dan nilainya.                                         |

### Struktur JSON `p_details_json`:

Array JSON harus berisi objek dengan key berikut:

- `parameter_code`: Kode parameter (misal: `PRM-002`).
- `val_num`: Nilai numerik (opsional, bisa `null`).
- `val_txt`: Nilai teks/keterangan (opsional, bisa `null`).
- `val_bool`: Nilai boolean (1/0, opsional, bisa `null`).

---

## 3. Contoh Penggunaan (Query CALL)

### Skenario: Brazing Furnace (OK)

```sql
CALL sp_insert_trace_data(
    'ISS-99901',
    1,
    'PRC-005',
    '[
        {"parameter_code": "PRM-002", "val_num": 42.5, "val_txt": null, "val_bool": null},
        {"parameter_code": "PRM-003", "val_num": 610.2, "val_txt": null, "val_bool": null}
    ]'
);
```

### Skenario: Leakage Testing (NG)

```sql
CALL sp_insert_trace_data(
    'ISS-99902',
    0,
    'PRC-007',
    '[
        {"parameter_code": "PRM-008", "val_num": 0.50, "val_txt": null, "val_bool": null},
        {"parameter_code": "PRM-050", "val_num": null, "val_txt": null, "val_bool": 0}
    ]'
);
```

---

## 4. Troubleshooting

### Error "Process Code not found"

Artinya kode proses yang lu kirim tidak terdaftar di tabel `processes`. Cek daftar kode yang tersedia:

```sql
SELECT code, name FROM processes;
```

### Error "Parameter Code" (Data tidak masuk detail)

Jika header masuk tapi detail kosong, pastikan `parameter_code` di dalam JSON cocok dengan yang ada di tabel `parameters`:

```sql
SELECT code, name FROM parameters;
```

### Atomicity

Prosedur ini menggunakan `START TRANSACTION`. Jika terjadi error pada salah satu parameter (misal kode tidak ditemukan), maka seluruh data (header & detail) tidak akan disimpan ke database.

---

## 5. Stored Procedure Source Code

Gunakan script ini untuk membuat/update prosedur di database MySQL:

```sql
DROP PROCEDURE IF EXISTS sp_insert_trace_data;
DROP PROCEDURE IF EXISTS sp_record_manufacturing_log;

DELIMITER $$

CREATE PROCEDURE sp_insert_trace_data(
    IN p_issue_no VARCHAR(100),
    IN p_is_ok TINYINT(1),
    IN p_process_code VARCHAR(50),
    IN p_details_json JSON
)
BEGIN
    DECLARE v_log_id BIGINT;
    DECLARE v_process_id INT;
    DECLARE v_final_issue_no VARCHAR(110);
    DECLARE i INT DEFAULT 0;
    DECLARE v_count INT;
    
    -- 1. Penentuan Suffix -R berdasarkan p_is_ok
    IF p_is_ok = 0 THEN
        SET v_final_issue_no = CONCAT(p_issue_no, '-R');
    ELSE
        SET v_final_issue_no = p_issue_no;
    END IF;

    -- 2. Lookup Process ID
    SELECT id INTO v_process_id FROM processes WHERE code = p_process_code LIMIT 1;
    IF v_process_id IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Process Code not found';
    END IF;

    START TRANSACTION;

    -- 3. LOGIC UPSERT HEADER:
    -- Cari apakah sudah ada header dengan IssueNo ini yang masih aktif
    SELECT id INTO v_log_id FROM process_logs WHERE issue_no = v_final_issue_no AND is_active = 1 LIMIT 1;

    IF v_log_id IS NULL THEN
        -- Kalau belum ada, baru buat header baru
        INSERT INTO process_logs (issue_no, is_active, created_at)
        VALUES (v_final_issue_no, 1, NOW());
        SET v_log_id = LAST_INSERT_ID();
    ELSE
        -- Kalau sudah ada, update waktu terakhir diupdate
        UPDATE process_logs SET updated_at = NOW() WHERE id = v_log_id;
    END IF;

    -- 4. Loop Insert Detail (Nambah baris baru untuk setiap proses)
    SET v_count = JSON_LENGTH(p_details_json);
    WHILE i < v_count DO
        INSERT INTO process_log_details (
            process_log_id, 
            process_id, 
            parameter_id, 
            value_number, 
            value_text, 
            value_boolean, 
            created_at
        )
        SELECT 
            v_log_id,
            v_process_id,
            p.id,
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_num'))), 'null'),
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_txt'))), 'null'),
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_bool'))), 'null'),
            NOW()
        FROM parameters p 
        WHERE p.code = JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].parameter_code')))
        LIMIT 1;

        SET i = i + 1;
    END WHILE;

    COMMIT;

    SELECT v_log_id AS log_id;
END $$

DELIMITER ;
```
