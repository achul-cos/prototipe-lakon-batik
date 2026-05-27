<div align="center">

# 🎨 Lakon Batik

### Game Desktop — Unity 2021 | Batik Crafting Simulation

[![Unity](https://img.shields.io/badge/Unity-2022%20LTS-black?style=for-the-badge&logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-blue?style=for-the-badge)](https://unity.com/)
[![Status](https://img.shields.io/badge/Status-Prototype-orange?style=for-the-badge)]()
[![Event](https://img.shields.io/badge/KMIPN%202026-Pengembangan%20Aplikasi%20Permainan-purple?style=for-the-badge)]()

<br/>

> *"Di tangan Mpu Canting, sehelai kain menjadi cerita."*

<br/>

**Lakon Batik** adalah game simulasi membatik berbasis desktop yang mengajak pemain mengenal industri batik rumahan melalui pengalaman bermain yang santai dan naratif. Dikembangkan untuk kompetisi **KMIPN 2026** cabang Pengembangan Aplikasi Permainan.

</div>

---

## 📖 Daftar Isi

- [Tentang Game](#-tentang-game)
- [Gameplay](#-gameplay)
- [Fitur](#-fitur)
- [Pola Batik](#-pola-batik)
- [Arsitektur & Design Pattern](#-arsitektur--design-pattern)
- [Struktur Proyek](#-struktur-proyek)
- [Cara Menjalankan](#-cara-menjalankan)
- [Teknologi](#-teknologi)
- [Tim Pengembang](#-tim-pengembang)

---

## 🏮 Tentang Game

Lakon Batik hadir dengan pendekatan **consumer-centered** dan **naratif storytelling** untuk mengenalkan generasi muda pada kekayaan budaya batik Indonesia. Game ini dirancang sebagai pengalaman *relaxing* — cocok dimainkan setelah pulang kerja tanpa tekanan kompetitif.

### Lore

> Suatu hari, **Mpu Canting** membutuhkan uang. Dengan keahlian membatik yang telah ia miliki seumur hidup, ia memutuskan untuk membuka toko batik di rumahnya sendiri. Toko dibuka setiap hari **Senin hingga Jumat, pukul 09.00–17.00**. Setiap hari pelanggan baru datang membawa cerita dan keinginan mereka masing-masing — dan tugasmu adalah mewujudkan batik impian mereka.

*Note : Ini masih placeholder lore dari tim programmer, README.md akan disesuaikan kembali dengan tim proyek lainya*

### Target Pemain

| Aspek | Detail |
|-------|--------|
| Target usia | Generasi muda (17–30 tahun) |
| Genre | Simulation / Casual / Relaxing |
| Tone | Hangat, naratif, tidak stressful |
| Platform | Desktop (Windows / Mac / Linux) |

---

## 🎮 Gameplay

Satu siklus penuh melayani seorang pelanggan terdiri dari lima tahap:

```
┌─────────────────────────────────────────────────────────┐
│                    ALUR SATU HARI                       │
│                                                         │
│  [Lobby] ──► [Dialog] ──► [Menggambar] ──► [Mewarnai]   │
│     ▲                                          │        │
│     └──────────── [Menjemur] ◄─────────────────┘        │
│                       │                                 │
│                   [Hasil & Bayaran]                     │
│                       │                                 │
│               [Ulangi / Hari Berikutnya]                │
└─────────────────────────────────────────────────────────┘
```

### Penjelasan Tiap Tahap

#### 🏠 Lobby — Area Tunggu
- Maksimal **3 pelanggan** menunggu di kursi secara bersamaan
- Waktu game berjalan: **8 jam game = 20 menit nyata** (09.00–17.00)
- Kalender menampilkan hari, cuaca hari ini, dan ramalan cuaca besok
- Tekan `ESC` untuk pause — muncul animasi **pocket clock** dengan jarum jam

#### 💬 Dialog — Visual Novel Style
- Pelanggan menceritakan kisah hidupnya dan batik yang diinginkan
- Teks muncul dengan efek **typewriter**
- **Keyword pola** (warna oranye) dan **keyword warna** (warna biru) di-highlight otomatis sebagai petunjuk

#### ✏️ Menggambar — Batik Canvas
- Gambari pola batik di atas kanvas sesuai panduan samar yang diberikan
- **3 ukuran canting**: Kecil (r=3px), Sedang (r=7px), Besar (r=12px)
- Pola samar muncul 5 detik di awal, lalu menghilang
- Pola samar muncul kembali (fade in 2 detik) jika player diam
- Progress hanya naik jika menggambar **di atas area pola** — bukan di luar pola
- Tombol "Selesai" muncul otomatis saat coverage ≥ 80%

#### 🎨 Mewarnai — Metode Celup
- Racik warna menggunakan **slider RGB** (Merah, Hijau, Biru)
- Atur kedalaman celup — sebagian atau seluruh kain
- Piksel hitam (pola) tidak tertimpa warna — hanya area putih yang diwarnai

#### ☀️ Menjemur
- Waktu ideal jemur: **5 detik** (window ±0.75 detik)
- Maksimal: **10 detik** — lewat dari itu kain pudar dan skor berkurang
- Angkat terlalu cepat: kain masih basah, opacity rendah

#### 📊 Hasil & Pembayaran

| Skor | Respons Pelanggan | Bayaran |
|------|------------------|---------|
| < 50% | Protes keras, kecewa | 50% harga |
| 50–74% | Kritik halus, kurang puas | 100% harga |
| 75–89% | Berterima kasih | 100% harga |
| ≥ 90% | Memuji, sangat puas | 120% harga (bonus!) |

### Sistem Cuaca & Pelanggan

Jumlah pelanggan harian (5–15 orang) dipengaruhi dua faktor:

| Cuaca | Multiplier | Hari | Multiplier |
|-------|-----------|------|-----------|
| ☀️ Cerah | 1.00× | Jumat | 1.00× |
| ⛅ Berawan | 0.85× | Kamis | 0.95× |
| 🌧️ Gerimis | 0.65× | Rabu | 0.85× |
| 🌧️ Hujan | 0.45× | Selasa | 0.75× |
| ⛈️ Badai | 0.25× | Senin | 0.70× |

---

## ✨ Fitur

- [x] **Save System** — Multiple save slot dengan nama toko unik
- [x] **Sistem Waktu** — 8 jam game = 20 menit nyata, bisa di-pause
- [x] **Pocket Clock** — Animasi jam kantung saat pause dengan jarum jam akurat
- [x] **Cuaca Dinamis** — 5 kondisi cuaca yang mempengaruhi jumlah pelanggan
- [x] **Dialog Visual Novel** — Typewriter effect + keyword highlight otomatis
- [x] **Batik Drawing System** — Brush berbasis piksel dengan 3 ukuran canting
- [x] **Guide Overlay** — Pola samar dengan sistem idle fade-in/fade-out
- [x] **Coverage Detection** — Progress hanya terhitung di area pola (bukan seluruh canvas)
- [x] **Dyeing System** — Pewarnaan celup dengan mixer RGB
- [x] **Drying Mini-game** — Timing berbasis waktu nyata dengan efek visual
- [x] **Scoring Engine** — Formula multi-faktor (pola + warna + jemur)
- [x] **10 Karakter Pelanggan** — Masing-masing dengan backstory dan dialog unik
- [x] **Settings** — Volume (Master/Music/SFX), resolusi, kualitas grafis, fullscreen
- [x] **Cutscene** — Pengenalan lore dengan efek typewriter dan fade

*Note : Ini masih rencana rancangan dari tim programmer, README.md akan disesuaikan kembali dengan tim proyek lainya*

---

## 🖼️ Pola Batik

Lima pola batik asli Indonesia yang tersedia dalam game:

| # | Pola | Asal | Keyword Dialog | Difficulty |
|---|------|------|---------------|-----------|
| 1 | **Mega Mendung** | Cirebon | awan, mendung, langit, mega | ⭐⭐ |
| 2 | **Parang** | Yogyakarta / Solo | parang, ombak, keris, diagonal | ⭐⭐⭐ |
| 3 | **Kawung** | Yogyakarta | kawung, buah, bulat, aren | ⭐⭐ |
| 4 | **Truntum** | Solo | truntum, bunga, bintang, kecil | ⭐⭐⭐⭐ |
| 5 | **Sekar Jagad** | Solo / Yogyakarta | sekar jagad, peta, jagad, dunia | ⭐⭐⭐⭐⭐ |

*Note : Ini masih rencana rancangan dari tim programmer, README.md akan disesuaikan kembali dengan tim proyek lainya*

---

## 🏗️ Arsitektur & Design Pattern

Proyek ini menggunakan **Singleton Design Pattern** sebagai fondasi arsitektur, dengan pendekatan event-driven untuk komunikasi antar sistem.

### Singleton Manager

```
GameManager        — State game, transisi scene, uang, pause
TimeManager        — Konversi jam game ↔ waktu nyata, event tutup toko  
SaveSystem         — Read/Write JSON ke persistentDataPath
WeatherSystem      — Generasi cuaca & kalkulasi jumlah pelanggan
CustomerManager    — Queue spawning pelanggan harian
BatikDatabase      — Registry BatikPattern ScriptableObject
SettingsManager    — Volume AudioMixer, resolusi, kualitas grafis
ResultManager      — Kalkulasi skor & tampilan hasil
```

### Alur State Game

```
MainMenu → Cutscene → Lobby ⟲
                        ↓ (pilih pelanggan)
                      Dialog
                        ↓
                    BatikDrawing
                        ↓
                      Dyeing
                        ↓
                      Drying
                        ↓
                      Result
                        ↓
                    (kembali ke Lobby)
```

### Formula Scoring

```
raw_score  = (drawing_accuracy × 0.55)
           + (color_accuracy   × 0.30)
           + (dry_quality      × 0.15)

final_score = raw_score × lerp(1.0, difficulty_multiplier, 0.5)
```

---

## 📁 Struktur Proyek

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── TimeManager.cs
│   │   ├── SaveSystem.cs
│   │   ├── WeatherSystem.cs
│   │   ├── CustomerManager.cs
│   │   ├── BatikDatabase.cs
│   │   ├── SettingsManager.cs
│   │   └── ResultManager.cs
│   ├── Gameplay/
│   │   ├── Batik/
│   │   │   ├── BatikCanvas.cs
│   │   │   └── BatikDrawingManager.cs
│   │   ├── Coloring/
│   │   │   └── DyeingManager.cs
│   │   ├── Drying/
│   │   │   └── DryingManager.cs
│   │   ├── Dialog/
│   │   │   ├── DialogManager.cs
│   │   │   └── CustomerOrder.cs
│   │   └── Lobby/
│   │       └── CustomerController.cs
│   ├── UI/
│   │   ├── MainMenuController.cs
│   │   ├── LobbyUI.cs
│   │   ├── PauseClockUI.cs
│   │   ├── CutsceneManager.cs
│   │   ├── DialogSceneBootstrap.cs
│   │   └── UIManager.cs
│   ├── Data/
│   │   ├── BatikPattern.cs         (ScriptableObject)
│   │   └── SaveData.cs
│   └── Utils/
│       ├── Singleton.cs
│       ├── TextureUtils.cs
│       ├── ColorUtils.cs
│       └── ScoringEngine.cs
├── Scenes/
│   ├── 00_MainMenu.unity
│   ├── 01_Cutscene.unity
│   ├── 02_Lobby.unity
│   ├── 03_Dialog.unity
│   ├── 04_BatikDrawing.unity
│   ├── 05_Dyeing.unity
│   ├── 06_Drying.unity
│   └── 07_Result.unity
├── ScriptableObjects/
│   ├── MegaMendung.asset
│   ├── Parang.asset
│   ├── Kawung.asset
│   ├── Truntum.asset
│   └── SekarJagad.asset
├── Prefabs/
│   ├── CustomerPrefab.prefab
│   └── SaveEntry.prefab
├── Textures/
│   └── BatikPatterns/
│       ├── mask_mega_mendung.png
│       ├── mask_parang.png
│       ├── mask_kawung.png
│       ├── mask_truntum.png
│       └── mask_sekar_jagad.png
└── Audio/
    ├── Music/
    └── SFX/
```

---

## 🚀 Cara Menjalankan

### Prasyarat

| Software | Versi |
|---------|-------|
| Unity Editor | 2022 LTS (2022.3.62f3) |
| TextMeshPro | Termasuk dalam Unity Package Manager |
| Platform Module | Windows / Mac / Linux Standalone |

### Clone & Buka Proyek

```bash
# Clone repository
git clone https://github.com/username/lakon-batik.git

# Buka Unity Hub
# Klik "Add project from disk"
# Arahkan ke folder hasil clone
```

### Setup Pertama Kali

```
1. Buka Unity 2022 LTS
2. Tunggu import aset selesai
3. Window → TextMeshPro → Import TMP Essential Resources
4. File → Build Settings → pastikan semua scene sudah terdaftar
   dengan urutan: 00_MainMenu, 01_Cutscene, 02_Lobby, dst.
5. Buka scene 00_MainMenu
6. Tekan Play ▶
```

### Build untuk Desktop

```
File → Build Settings
  Platform    : PC, Mac & Linux Standalone
  Target      : Windows 64-bit (atau sesuai OS)
  
Player Settings:
  Company Name : (nama tim)
  Product Name : Lakon Batik
  
Klik Build and Run
```

---

## 🛠️ Teknologi

| Teknologi | Kegunaan |
|-----------|---------|
| **Unity 2022 LTS** | Game engine utama |
| **C#** | Bahasa pemrograman |
| **TextMeshPro** | Sistem teks dengan rich text & keyword highlight |
| **Unity UI (uGUI)** | Antarmuka pengguna |
| **AudioMixer** | Sistem audio berlapis (Master / Music / SFX) |
| **JsonUtility** | Serialisasi data save game |
| **Texture2D API** | Sistem menggambar batik berbasis piksel |
| **ScriptableObject** | Data pola batik yang modular |
| **Singleton Pattern** | Manajemen state global antar scene |

---

## 👥 Tim Pengembang

Dikembangkan untuk kompetisi **KMIPN 2026** — Cabang Lomba Pengembangan Aplikasi Permainan.

| Peran | Nama |
|-------|------|
| Game Programmer | *Nasrullah (Achul)* |
| Game Designer | *M Riandi Rizky* | *Erlangga* |
| Artist | *Jufry* | *Dan lainya* |
| Project Manager | *BERLIANSYAH RUMODHON, S.Pd.,M.Sn* |

---

## 📄 Lisensi

Proyek ini dikembangkan untuk keperluan kompetisi akademik **KMIPN 2026**.  
Seluruh aset visual dan audio bersifat original atau menggunakan lisensi bebas.  
Motif batik yang digunakan merupakan warisan budaya Indonesia.

---

<div align="center">

Dibuat dengan ❤️ dan secangkir kopi

*Lestarikan batik, lestarikan budaya Indonesia.*

</div>
