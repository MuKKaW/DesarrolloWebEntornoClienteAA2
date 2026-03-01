# 🏆 Plataforma de Subastas Online

Sistema completo de subastas con **Vue.js 3 + TypeScript**, **.NET Core 8**, **MySQL** y **Docker**.

---

## 🚀 Inicio Rápido

### Requisitos previos
- ✅ Docker y Docker Compose instalados
- ✅ Git configurado

### Comando único para levantar todo:

```bash
docker compose build
docker compose up
```

Esto levantará automáticamente:
- **Frontend Vue.js**: http://localhost:3000
- **API .NET Swagger**: http://localhost:8080/swagger
- **MySQL Database**: localhost:3306

---

## 📋 Accesos

| Servicio | URL | Usuario | Contraseña |
|----------|-----|---------|------------|
| **Frontend** | http://localhost:3000 | - | - |
| **API Swagger** | http://localhost:8080/swagger | - | - |
| **MySQL** | localhost:3306 | app | app |

---

## 🏗️ Estructura del Proyecto

```
DesarrolloWebEntornoClienteAA2/
├── docker-compose.yml          # Orquestación de servicios
├── aa-manuel-rivera/           # Frontend Vue.js
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── src/
│   │   ├── stores/            # Pinia state management
│   │   ├── components/        # Componentes Vue
│   │   ├── views/             # Páginas/Vistas
│   │   ├── router/            # Vue Router
│   │   ├── i18n/              # Traducciones (ES/EN)
│   │   ├── services/          # API client (axios)
│   │   └── types/             # TypeScript interfaces
│   └── package.json
│
├── back/                       # Backend .NET Core
│   ├── Dockerfile
│   ├── Subastas.Api/
│   │   ├── Program.cs         # Endpoints principales
│   │   ├── models/            # User, Product, Bid
│   │   ├── data/              # DbContext
│   │   ├── appsettings.json
│   │   └── Subastas.Api.csproj
│   └── package.json
│
└── db/
    └── init/
        └── 01_schema.sql      # Schema MySQL
```

---

## ✨ Funcionalidades Implementadas

### 📱 Frontend (Vue.js 3)
- ✅ Autenticación JWT (Login/Register)
- ✅ Gestión centralizada de estado (Pinia)
- ✅ Catálogo de productos con búsqueda y filtros
- ✅ Panel de administración (Productos, Usuarios)
- ✅ Dashboard con KPIs
- ✅ Validación de formularios (VeeValidate + Yup)
- ✅ Internacionalización (ES/EN con vue-i18n)
- ✅ Tema claro/oscuro (Tailwind CSS)
- ✅ Responsive design

### 🔧 Backend (.NET Core 8)
- ✅ API REST con endpoints CRUD
- ✅ Autenticación JWT
- ✅ Encriptación de contraseñas (BCrypt)
- ✅ Paginación de listados
- ✅ Filtrado y búsqueda
- ✅ Validación de datos
- ✅ CORS configurado
- ✅ Health checks

### 📊 Base de Datos
- ✅ MySQL 8.0
- ✅ Schema predefinido
- ✅ Relaciones entre tablas
- ✅ Inicialización automática

---

## 🛠️ Comandos Disponibles

### Desarrollo local (sin Docker)

**Frontend:**
```bash
cd aa-manuel-rivera
npm install
npm run dev
```

**Backend:**
```bash
cd back/Subastas.Api
dotnet restore
dotnet run
```

### Producción (Docker)

**Build y levantar:**
```bash
docker compose build
docker compose up
```

**Detener servicios:**
```bash
docker-compose down
```

**Ver logs:**
```bash
docker-compose logs -f
```

---

## 🔐 Seguridad

- 🔒 Contraseñas encriptadas con BCrypt
- 🔑 JWT tokens de 7 días de expiración
- 🛡️ CORS configurado para localhost
- ✅ Validación de entrada en cliente y servidor

---

## 📊 Endpoints API

### Autenticación
- `POST /api/auth/login` - Iniciar sesión
- `POST /api/auth/register` - Registrarse

### Productos
- `GET /api/products` - Listar (con paginación/filtros)
- `GET /api/products/{id}` - Detalle
- `POST /api/products` - Crear
- `PUT /api/products/{id}` - Actualizar
- `DELETE /api/products/{id}` - Eliminar

### Usuarios
- `GET /api/users` - Listar
- `GET /api/users/{id}` - Detalle
- `POST /api/users` - Crear
- `PUT /api/users/{id}` - Actualizar
- `DELETE /api/users/{id}` - Eliminar

### Pujas
- `GET /api/bids` - Listar pujas
- `POST /api/bids` - Crear puja
- `GET /api/products/{id}/bids` - Pujas de un producto

### Estadísticas
- `GET /api/statistics` - KPIs
- `GET /api/statistics/bids-by-date` - Pujas por fecha
- `GET /api/statistics/top-products` - Top productos

---

## 🐛 Solución de Problemas

### Puerto ya en uso
```bash
netstat -ano | findstr :3000  # Windows
netstat -ano | grep :3000    # Linux/Mac
```

### Limpiar Docker
```bash
docker system prune -a
docker volume prune
```

### Reiniciar servicios
```bash
docker-compose restart
```

---

## 📝 Notas de Desarrollo

- **JWT Secret**: Cambiar en `Program.cs` antes de producción
- **Database**: Credenciales en variables de entorno del compose
- **Logs**: Ver con `docker-compose logs [servicio]`
- **Hot Reload**: Disponible en desarrollo local

---

**Creado:** febrero 2026  
**Stack:** Vue 3 + TypeScript + .NET 8 + MySQL + Docker
