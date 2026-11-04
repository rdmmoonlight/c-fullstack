# c-fullstack
Semua bahasa C yg mungkin dipakai

'''
c-fullstack-monorepo/
├── docker-compose.yml
├── README.md
├── nginx/
│   └── nginx.conf
├── backend-cpp/              # Service Backend (C++ HTTP API)
│   ├── Dockerfile
│   ├── CMakeLists.txt
│   └── main.cpp
├── frontend-web/             # Web Client (C / WebAssembly via Emscripten)
│   ├── Dockerfile
│   ├── index.html
│   └── main.c
└── mobile-android/           # Android App (C# via .NET MAUI / Android)
    └── MainActivity.cs

