You can also have a gist that is built for this project:
[Gist Repo](https://gist.github.com/Ryan-PG/d3093114050f38cf2542f6b34e21850c)

# 🚀 .NET 8 API with Dapr Pub/Sub on Minikube

This project demonstrates a **minimal .NET 8 Web API** that uses **Dapr's Pub/Sub** building block with **Redis** as the message broker — all deployed in **Minikube** using Kubernetes.

---

## 🎯 Features

- ✅ ASP.NET Core 8 Web API (single project)
- ✅ Publishes and Subscribes to messages via Dapr
- ✅ Dapr sidecar auto-injected via K8s annotations
- ✅ Redis as Pub/Sub broker (runs inside Minikube)
- ✅ Dockerized and deployed in Minikube
- ✅ Fully local + offline setup — no external dependencies

---

## 🧰 Prerequisites

- [Minikube](https://minikube.sigs.k8s.io/docs/start/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [.NET SDK 8](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://docs.docker.com/get-docker/)
- [Helm (optional, for Redis)](https://helm.sh/docs/intro/install/)

---

## ⚙️ Setup Guide

### 1. Start Minikube and Use Its Docker

```bash
minikube start
# minikube -p minikube docker-env --shell=cmd | Invoke-Expression
````

also use inside-minikube docker:

```bash
& minikube -p minikube docker-env | Invoke-Expression
```

---

### 2. Initialize Dapr in Minikube

```bash
dapr init -k
```

---

### 3. Deploy Redis (for Pub/Sub)

**Option 1: Using Helm**

```bash
helm repo add bitnami https://charts.bitnami.com/bitnami
helm install redis bitnami/redis --set architecture=standalone
```

**Option 2: Using YAML**

```yaml
# redis.yaml
apiVersion: v1
kind: Service
metadata:
  name: redis
spec:
  ports:
    - port: 6379
  selector:
    app: redis
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
        - name: redis
          image: redis:6
          ports:
            - containerPort: 6379
```

```bash
kubectl apply -f redis.yaml
```

#### **Then Deploy Pub/Sub Component for Redis Using YAML**

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
  namespace: default
spec:
  type: pubsub.redis
  version: v1
  metadata:
    - name: redisHost
      value: redis.default.svc.cluster.local:6379
```

```bash
kubectl apply -f pubsub.yaml
```

---

### 4. Create the .NET Project

```bash
dotnet new webapi -n dapr-sample-net-project
cd dapr-sample-net-project
dotnet add package Dapr.Client
dotnet add package Dapr.AspNetCore
```

---

### 5. Controllers

#### 📤 `PublishController.cs`

```csharp
[ApiController]
[Route("publish")]
public class PublishController : ControllerBase
{
    private readonly DaprClient _daprClient;

    public PublishController(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    [HttpPost("order")]
    public async Task<IActionResult> PublishOrder([FromBody] Order order)
    {
        await _daprClient.PublishEventAsync("pubsub", "neworder", order);
        Console.WriteLine($"📤 Published Order: {order.Id}");
        return Ok(new { message = "Order published successfully!" });
    }
}

public class Order
{
    public string Id { get; set; }
    public string Product { get; set; }
}
```

#### 📥 `SubscriberController.cs`

```csharp
using Dapr;


[ApiController]
[Route("orders")]
public class SubscriberController : ControllerBase
{
	[Topic("pubsub", "neworder")]
    [HttpPost("neworder")]
    public IActionResult HandleNewOrder([FromBody] Order order)
    {
        Console.WriteLine($"📥 Received Order: {order.Id} - {order.Product}");
        return Ok();
    }
}
```

---

### 6. Update `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

# Dapr
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

# Dapr
app.MapSubscribeHandler();

app.Run();
```

---

### 7. Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "dapr-sample-net-project.csproj"
RUN dotnet build "dapr-sample-net-project.csproj" -c Release -o /app/build
RUN dotnet publish "dapr-sample-net-project.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "dapr-sample-net-project.dll"]
```

---

### 8. Kubernetes Deployment

create `app-deployment.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dapr-sample-api
spec:
  replicas: 1
  selector:
    matchLabels:
      app: dapr-sample-api
  template:
    metadata:
      labels:
        app: dapr-sample-api
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "dapr-sample-api"
        dapr.io/app-port: "8080"
    spec:
      containers:
        - name: dapr-sample-api
          image: dapr-sample-api
          imagePullPolicy: Never
          ports:
            - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: dapr-sample-api
spec:
  selector:
    app: dapr-sample-api
  ports:
    - protocol: TCP
      port: 80
      targetPort: 8080
```

---

### 9. Build Image Inside Minikube

```bash
docker build -t dapr-sample-api .
```

---

### 10. Deploy to Kubernetes

```bash
kubectl apply -f app-deployment.yaml
```

---

### 11. Port Forward to Test

```bash
kubectl port-forward svc/dapr-sample-api 8080:80
```

---

### 12. Test Pub/Sub

```powershell
$body = @{
    id = "123"
    product = "AI-powered Keyboard"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/publish/order" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

✅ Console should show:

```
📥 Received Order: 123 - AI-powered Keyboard
```

---

## 🧼 Cleanup

```bash
kubectl delete deployment dapr-sample-api
kubectl delete service dapr-sample-api
helm uninstall redis # or kubectl delete -f redis.yaml
```

---

## 📚 Credits & Stack

- .NET 8
    
- Dapr 1.15+
    
- Redis (as pub/sub broker)
    
- Kubernetes (via Minikube)
    
- PowerShell (for local testing)
    
- Dapr SDK for .NET
    

---

## 💡 Want More?

> Add declarative subscriptions, connect multiple services, or use cloud-native event brokers like Azure Service Bus or Kafka!

---

Built with ❤️ by Ryan Heida & ChatGPT

```

Let me know if you want this saved as a file or auto-generated into a GitHub repo layout. 🧠💾
```

---
## 🧩 Optional: Part 2 – Dapr Pub/Sub with Multiple Subscribers

To have multiple services receive the same message from a single publisher:

### 1. Use Dapr's Pub/Sub component (e.g., Redis)

Make sure Redis is deployed and a `pubsub` component is installed in your cluster.

---

### 2. Create multiple subscriber services

Each subscriber:
- Has its own `app-id`
- Listens on the same topic (e.g., `"neworder"`)

Example apps:

| Project               | Dapr App ID         |
|-----------------------|---------------------|
| `subscriber-1`        | `order-processor`   |
| `subscriber-2`        | `email-service`     |
| `subscriber-3`        | `analytics-engine`  |

Each service has a controller like:

```csharp
[ApiController]
[Route("orders")]
public class SubscriberController : ControllerBase
{
    [HttpPost("neworder")]
    public IActionResult HandleNewOrder([FromBody] Order order)
    {
        Console.WriteLine($"📥 Received Order: {order.Id} - {order.Product}");
        return Ok();
    }
}
````

---

### 3. Annotate each deployment with its own Dapr `app-id`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: YOUR-DEPLOYMENT-NAME            # ✅ UNIQUE: e.g. order-api
spec:
  replicas: 1
  selector:
    matchLabels:
      app: YOUR-DEPLOYMENT-NAME         # ✅ SAME as metadata.name
  template:
    metadata:
      labels:
        app: YOUR-DEPLOYMENT-NAME       # ✅ SAME as above
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "UNIQUE-DAPR-ID"     # ✅ UNIQUE DAPR APP ID
        dapr.io/app-port: "8080"
    spec:
      containers:
        - name: dapr-sample-api         # 🔁 this can stay same
          image: dapr-sample-api        # 🔁 using the same image
          imagePullPolicy: Never
          ports:
            - containerPort: 8080

---
apiVersion: v1
kind: Service
metadata:
  name: YOUR-SERVICE-NAME               # ✅ UNIQUE SERVICE
spec:
  selector:
    app: YOUR-DEPLOYMENT-NAME           # ✅ MATCH the label above
  ports:
    - protocol: TCP
      port: 80
      targetPort: 8080
```


### ✅ Example: 3 Deployments Using Same Image

#### 📦 `order-api.yaml`

```yaml
metadata:
  name: order-api
  annotations:
    dapr.io/app-id: "order-api"
spec:
  containers:
    - name: dapr-sample-api
      image: dapr-sample-api
```

#### 📦 `email-api.yaml`

```yaml
metadata:
  name: email-api
  annotations:
    dapr.io/app-id: "email-api"
spec:
  containers:
    - name: dapr-sample-api
      image: dapr-sample-api
```

#### 📦 `log-api.yaml`

```yaml
metadata:
  name: log-api
  annotations:
    dapr.io/app-id: "log-api"
spec:
  containers:
    - name: dapr-sample-api
      image: dapr-sample-api
```

---

### 4. Deploy them

```bash
kubectl apply -f order-api.yaml
kubectl apply -f email-api.yaml
kubectl apply -f log-api.yaml
```

---
### 5. Port Forward to Test

```bash
kubectl port-forward svc/dapr-sample-api 8080:80
```

---
### 6. Publish to the topic

Use your main app or Dapr HTTP API:

```bash

$body = @{
    id = "123"
    product = "AI-powered Keyboard"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/publish/order" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"

```

✅ All 3 subscriber services will independently receive the event 🎉

```
