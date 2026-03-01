# Plataforma de Subastas Online

Este repositorio contiene una aplicacion de subastas hecha con Vue 3 y TypeScript en el frontend, .NET 8 en el backend y MySQL como base de datos. Todo el entorno se puede levantar con Docker.

La idea del proyecto es sencilla: un usuario puede registrarse, consultar subastas, entrar en el detalle de un producto y realizar pujas. Ademas, existe un panel de administracion para gestionar productos, usuarios, estadisticas y moderacion de subastas.

## Como arrancarlo

docker-compose build
docker compose up

Si todo va bien, al levantarlo tendras:

- Frontend: http://localhost:3000
- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- MySQL: localhost:3306

## Accesos y datos utiles

### Credenciales de base de datos

- Base de datos: `subastas`
- Usuario: `app`
- Password: `app`

### Usuarios de prueba

El backend crea datos semilla al arrancar. Por defecto quedan disponibles estos usuarios:

- Admin: `admin@admin.com` / `admin`
- Usuario normal: `user@user.com` / `user`

## Que funcionalidades incluye

### Parte publica

- Registro e inicio de sesion
- Catalogo de subastas
- Vista de detalle de producto
- Sistema de pujas
- Visualizacion del ultimo pujador por apodo
- Interfaz en espanol e ingles
- Tema claro y oscuro

### Panel de administracion

- Dashboard con estadisticas
- Gestion de productos
- Creacion de nuevas subastas
- Gestion de usuarios
- Moderacion y cancelacion de subastas activas

### Backend

- API REST
- Autenticacion con JWT
- Passwords cifradas con BCrypt
- Persistencia con Entity Framework Core y MySQL
- Carga automatica del esquema SQL

## Endpoints principales de la API

### Autenticacion

- `POST /api/auth/login`
- `POST /api/auth/register`

### Productos

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`
- `PUT /api/admin/products/{id}/cancel`

### Usuarios

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

### Pujas

- `GET /api/bids`
- `POST /api/bids`
- `GET /api/products/{id}/bids`

### Estadisticas

- `GET /api/statistics`
- `GET /api/statistics/bids-by-date`
- `GET /api/statistics/top-products`