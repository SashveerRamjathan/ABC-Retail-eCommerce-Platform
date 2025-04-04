# 🌍 ABC Retail - E-Commerce Platform with Azure Cloud Integration

**ABC Retail** is a robust e-commerce web application designed to handle large volumes of transactions, product information, and customer orders. Developed as part of my Cloud Development B module, this project addresses the challenges faced by ABC Retail, a growing online retailer, by integrating a range of Azure cloud services to ensure scalability, reliability, and efficiency.

## 🚀 Project Overview
ABC Retail is designed to manage and optimize the company's order processing system and inventory management. The platform leverages various Azure services to streamline the company's operations and improve customer experience. The goal was to migrate from a traditional on-premises infrastructure to a cloud-first, scalable solution using Azure.

The project includes:
- Migration of order processing to the cloud
- Real-time event processing
- Scalable storage for product data and customer information
- Enhanced messaging and order management capabilities
- Optimized data analytics for better decision-making

## 🧩 Features
### ✅ Core Functionality
- **User-friendly interface** with easy navigation and smooth shopping experience
- **Product catalog** with detailed images, descriptions, prices, and availability
- **Customer profiles** stored securely in Azure Tables
- **Order management system** for customers to place and track orders
- **Inventory management system** for updating product availability
- **Multimedia support** for product images hosted on Azure Blob Storage

### 💡 Azure Integration
- **Azure App Service**: Hosts the web application for seamless accessibility
- **Azure SQL Database**: Centralized storage for customer, product, and order information
- **Azure Functions**: Handles background processing tasks like order confirmations and updates
- **Azure Blob Storage**: Hosts product images and multimedia content
- **Azure Queue Storage**: Manages messages related to order processing and inventory updates
- **Azure Logic Apps**: Automates email notifications to customers after successful orders

## 🛠️ Technologies Used
### Layer | Tools / Tech
- **Frontend**: ASP.NET Core MVC, Razor Pages, Bootstrap
- **Backend**: ASP.NET Core, C#
- **Database**: Microsoft SQL Server (Azure-hosted)
- **Cloud Platform**: Microsoft Azure
- **Dev Tools**: Visual Studio 2022, Git, GitHub
- **Other Services**: Azure App Service, Azure Functions and Azure Logic Apps

## 🌐 Deployment
The application was deployed to **Microsoft Azure** using a Platform-as-a-Service (PaaS) model with the following architecture:

- **Frontend + Backend**: ASP.NET Core MVC Web App hosted on Azure App Service
- **Database**: Azure SQL Database for relational data storage (users, products, orders)
- **Serverless Features**:
  - **Logic Apps**: Automates customer notifications
  - **Azure Functions**: Executes event-driven workflows such as order confirmations

## 📈 Key Takeaways
- **Cloud-first architecture**: Leveraging Azure's scalability and reliability for growing businesses
- **PaaS Integration**: Using Azure App Service, SQL Database, Functions, and Logic Apps for a seamless cloud experience
- **Secure and scalable development**: Ensuring the platform can handle peak traffic and transaction volumes

