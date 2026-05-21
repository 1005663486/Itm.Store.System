# Guía Paso a Paso: Generación de QR, Configuración de Kubernetes y Pruebas de Carga

Esta guía documenta todo el proceso que hemos realizado en el proyecto de `Itm.Store.System`. Está diseñada para que los estudiantes puedan replicar cada uno de los pasos en sus propios entornos.

---

## 🚀 Paso 1: Generación de Códigos QR en Azure Functions

Implementamos la lógica para generar un código QR cada vez que se procesa una orden exitosamente mediante RabbitMQ.

1. **Instalar el paquete de generación QR:**
   Nos posicionamos en la carpeta de `Itm.Tickets.Functions` e instalamos la biblioteca `QRCoder`:
   ```bash
   dotnet add Itm.Tickets.Functions/Itm.Tickets.Functions.csproj package QRCoder
   ```

2. **Actualizar la Lógica en `GenerateQrFunction.cs`:**
   Modificamos la función para generar un QR en base64 con los datos `OrderId`, `CustomerEmail` y `TotalAmount`, importando `using QRCoder;`.

3. **Subir archivos de configuración (Opcional):**
   Si se requiere el archivo `local.settings.json`, se fuerza su subida a git:
   ```bash
   git add -f Itm.Tickets.Functions/local.settings.json
   ```

---

## 📈 Paso 2: Configuración de Métricas en Kubernetes y Autoescalado (HPA)

Para que Kubernetes pueda escalar automáticamente nuestros contenedores (Horizontal Pod Autoscaler o HPA), necesita saber cuánta CPU/Memoria están consumiendo.

1. **Instalar Metrics Server (Para Docker Desktop):**
   Descargamos y aplicamos el configurador, y agregamos la bandera `--kubelet-insecure-tls` que es necesaria para entornos locales:
   ```bash
   kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

   kubectl patch deployment metrics-server -n kube-system --type='json' -p='[{"op": "add", "path": "/spec/template/spec/containers/0/args/-", "value": "--kubelet-insecure-tls"}]'
   ```

2. **Asignar Resources Requests a los Deployments:**
   El HPA necesita un límite base de CPU para calcular porcentajes de uso. A nuestros deployments les agregamos:
   ```yaml
   resources:
     requests:
       cpu: 100m
   ```

---

## 🐳 Paso 3: Creación de Dockerfiles para microservicios

Creamos configuraciones `Dockerfile` para las APIs restantes de forma que también podamos ejecutarlas en clúster.
Ubicamos un `Dockerfile` en la raíz de cada proyecto (`Itm.Inventory.Api`, `Itm.Product.Api`, `Itm.Gateway.Api`) usando la imagen oficial de .NET 8.

**Para compilar y subir a Docker Hub (Deben loguearse con `docker login` antes):**
```bash
# Compilar
docker build -t <tu_usuario_docker>/itm-inventory-api:latest -f Itm.Inventory.Api/Dockerfile .
docker build -t <tu_usuario_docker>/itm-product-api:latest -f Itm.Product.Api/Dockerfile .
docker build -t <tu_usuario_docker>/itm-gateway-api:latest -f Itm.Gateway.Api/Dockerfile .

# Subir a Docker Hub
docker push <tu_usuario_docker>/itm-inventory-api:latest
docker push <tu_usuario_docker>/itm-product-api:latest
docker push <tu_usuario_docker>/itm-gateway-api:latest
```

---

## ⚙️ Paso 4: Creación de Manifiestos YAML para Kubernetes

Creamos los archivos `.yaml` para desplegar nuestros microservicios recién empaquetados.
Creamos:
* `inventory-deployment.yaml`
* `product-deployment.yaml`
* `gateway-deployment.yaml`

Cada uno contiene un **Deployment** (con Resource Requests configurado a `100m`) y un **Service** que expone el microservicio internamente en el puerto `80`.

**Aplicar los manifiestos:**
```bash
kubectl apply -f inventory-deployment.yaml
kubectl apply -f product-deployment.yaml
kubectl apply -f gateway-deployment.yaml
```

---

## 🎯 Paso 5: Preparación y Ejecución de Pruebas de Carga con k6 y Auto-Escalado (HPA)

En este paso unimos el ataque de tráfico con `k6` y activamos las reglas para que el sistema crezca automáticamente frente a este tráfico. Creamos un archivo `test-load.js` para simular tráfico pesado y probar la escalabilidad del sistema. 

1. **El Script `test-load.js`:**
   Ataca el Ingress en el endpoint de órdenes (`http://api.itm-tickets.com/orders`) escalando hasta 100 peticiones concurrentes y manteniendo esa carga.

2. **Instalar k6:**
   * En Windows con Chocolatey: `choco install k6`
   * En Windows con Winget: `winget install k6`

3. **Activar el Auto-Escalado (HPA):**
   *🗣️ Guion Docente:*
   *"Vamos a configurar el HPA. Le diremos a Kubernetes: 'Si ves que mis pods están trabajando a más del 50% de su capacidad, tráeme refuerzos'."*

   Ejecutamos en la consola para activar el escalado en el deployment `order-api-deployment`:
   ```bash
   kubectl autoscale deployment order-api-deployment --cpu-percent=50 --min=3 --max=10
   ```

4. **Verificación en Tiempo Real (La Sala de Guerra):**
   Para que la clase sea dinámica, pedir a los estudiantes abrir **3 terminales en paralelo**.

   * **Terminal 1 (El Observador de Métricas):**
     Aquí verán cómo sube el % de CPU y aumenta la columna REPLICAS.
     ```bash
     kubectl get hpa order-api-deployment -w
     ```

   * **Terminal 2 (Los Pods en Combate):**
     Verán pods pasando de 'Pending' a 'Running' en segundos.
     ```bash
     kubectl get pods -l app=order-api -w
     ```

   * **Terminal 3 (El Ataque):**
     *Nota: Ya que estamos usando un certificado local auto-firmado, debes ejecutar el ataque de k6 ignorándolo.* Sólo debes añadir el flag `--insecure-skip-tls-verify`, y así el ataque de tráfico se ejecutará limpio:
     ```bash
     k6 run --insecure-skip-tls-verify test-load.js
     ```

   *🗣️ Guion Docente:*
   *"Miren la Terminal 1. Noten que hay un retraso de unos 15-30 segundos. Kubernetes no escala instantáneamente para evitar el 'efecto rebote'. Se llama Cooldown period. ¡Arquitectos, vean cómo nacen los nuevos pods para salvar el negocio!"*

---

## 🛡️ Paso 6: Fortalezas Digitales (Ciberseguridad y Gestión de Secretos - Clase 21)

Vamos a proteger nuestra infraestructura implementando **Rate Limiting** en el Gateway. Esto detendrá ataques de fuerza bruta respondiendo con un `429 Too Many Requests` si alguien abusa de nuestra API.

1. **Configurar Rate Limiting en el Gateway:**
   Hemos configurado en `Itm.Gateway.Api/Program.cs` una política que solo permite **10 peticiones cada 10 segundos por IP**.

2. **Probar el Escudo (Ataque con k6):**
   Usaremos un script de k6 rápido para intentar romper el torniquete digital. Este archivo se llama `attack.js`.

   Ejecuta lo siguiente:
   ```bash
   k6 run --vus 100 --duration 1s --insecure-skip-tls-verify attack.js
   ```

   **Resultado esperado:** Notarás que el sistema permite las primeras 10 transacciones en el log, y el resto serán bloqueos masivos y rápidos (`HTTP 429`). ¡Nuestros microservicios internos ni se enteran del ataque, ahorrando CPU y dinero en la nube!

### 🔐 Gestión de Secretos (Mejores prácticas):
Recuerden nunca guardar secretos como contraseñas en el archivo `appsettings.json`. En su lugar, el patrón correcto para entornos locales de desarrollo en .NET es usar los "User Secrets":

```bash
dotnet user-secrets set "DbPassword" "MiClaveSegura123" --project Itm.Gateway.Api/Itm.Gateway.Api.csproj
```
En producción, esto se sustituye cargando directamente las Variables de Entorno en el servidor o mediante servicios reales de bóveda segura como *Azure Key Vault*.
