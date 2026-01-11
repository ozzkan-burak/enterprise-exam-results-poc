graph TD
    User[Aday (React UI)] -- 1. Sorgu İsteği --> BFF[BFF Gateway (.NET 9)]
    
    subgraph "Secure Zone"
        BFF -- 2. Token & Saat Kontrolü --> Redis[(Redis Cache)]
        BFF -- 3. Uygunsa Kuyruğa At --> RabbitMQ[RabbitMQ]
        
        RabbitMQ -- 4. Sırayla İşle --> Worker[Worker Service]
        Worker -- 5. Sonucu Getir --> DB[(Mainframe DB)]
        Worker -- 6. Sonucu Cache'e Yaz --> Redis
    end
    
    Redis -.-> BFF
    BFF -.-> User
