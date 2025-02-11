# Проект Connector

## 📌 Обзор
Этот проект представляет собой C# коннектор для биржи Bitfinex, который получает данные о трейдах, свечах и тикерах, используя **REST и WebSocket API**. Также выполняется расчет баланса криптовалютного портфеля с выводом в **WPF DataGrid**.

## 🚀 Возможности
- **REST API Клиент** для получения трейдов, свечей и информации о тикерах.
- **WebSocket Клиент** для получения обновлений в реальном времени.
- **Расчет портфеля** с конверсией баланса в USDT, BTC, XRP, XMR и DASH.
- **WPF UI** для отображения портфеля.
- **Юнит и интеграционные тесты** с использованием `xUnit`.

## 📂 Структура проекта
```sh
Connector
│── Connector.API         # Основные API-клиенты (REST/WebSocket)
│── Connector.Tests       # Tесты
│── Connector.WPF         # WPF-приложение (паттерн MVVM)
│── README.md             # Документация проекта
```
## 🔧 Установка
### **1. Клонирование репозитория**
```sh
git clone https://github.com/BTwiiin/BitfinexConnector.git
cd Connector
```
### **2. Установка зависимостей**
```sh
dotnet restore
```
### **3. Запуск тестов**
```sh
dotnet test
```
### **4. Запуск проекта**
```sh
dotnet restore
dotnet run --project Connector.WPF
```
