# High Concurrency Exam Result Gateway (POC)

![Status](https://img.shields.io/badge/Architecture-Event--Driven-blue) ![Stack](https://img.shields.io/badge/.NET%209-React-purple)

### ⚠️ DISCLAIMER (YASAL UYARI)
This project is a **Proof of Concept (POC)** designed strictly for educational purposes to demonstrate **High Traffic Architecture** patterns. It is **NOT** associated with any official institution (ÖSYM, e-Devlet, etc.). All data used in this project is mock/fictitious generated via faker libraries.

---

### 🎯 The Problem
In high-demand scenarios (e.g., University Exam Results), millions of users attempt to access the system simultaneously. Traditional monolithic architectures fail due to database bottlenecks, resulting in timeouts and poor user experience.

### ✅ The Solution: Traffic Shaping & Asynchronous Processing
This architecture implements a **"Gatekeeper"** pattern:
1.  **BFF Layer (.NET 9):** Acts as a smart gateway. It validates requests based on "Time Slots" (ID-based throttling) before they reach the core system.
2.  **Queueing (RabbitMQ):** Instead of hitting the DB directly, requests are queued to flatten the traffic spike (Peak Shaving).
3.  **Caching (Redis):** Results are cached aggressively to prevent redundant processing.

### 🛠️ Tech Stack
* **Frontend:** React (Vite) - SPA
* **BFF/Gateway:** .NET 9 WebAPI
* **Message Broker:** RabbitMQ
* **Cache:** Redis
* **Core Logic:** .NET Worker Service

---

### 🏗️ Architecture

### 🏗️ Architecture

```mermaid
graph TD
    User["Aday (React UI)"] -- 1. HTTP İstek --> Middleware["Edge Middleware (Simulated Bouncer)"]
    
    subgraph "ExamResult.BFF (.NET 9)"
        Middleware -- 2a. Yasaklı Saat/ID (Red) --> Block["❌ 429 Too Many Requests"]
        Middleware -- 2b. İzinli --> Controller["API Controller"]
        Controller -- 3. Kuyruğa At (Publish) --> RabbitMQ["RabbitMQ Queue"]
    end
    
    subgraph "Backend Processing"
        RabbitMQ -- 4. Sırayla İşle (Consume) --> Worker["Worker Service"]
        Worker -- 5. Veriyi Getir --> DB[("Mock Database")]
        Worker -- 6. Cache'le --> Redis[("Redis")]
    end
