# MercatoApp

An ASP.NET Core Web Application built with Razor Pages.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later

## Getting Started

### Clone the repository

```bash
git clone https://github.com/SebastianDabkowski/mercato-3.git
cd mercato-3
```

### Build the application

```bash
dotnet build
```

### Run the application

```bash
dotnet run
```

The application will start and be available at `https://localhost:5001` or `http://localhost:5000`.

## Project Structure

```
├── Pages/              # Razor Pages
│   ├── Index.cshtml    # Home page
│   ├── Privacy.cshtml  # Privacy page
│   ├── Error.cshtml    # Error page
│   └── Shared/         # Shared layouts and partial views
├── wwwroot/            # Static files (CSS, JS, images)
├── Properties/         # Launch settings
├── Program.cs          # Application entry point
├── appsettings.json    # Application configuration
└── MercatoApp.csproj   # Project file
```

## Features

- ASP.NET Core Razor Pages
- Bootstrap 5 for responsive UI
- Static file serving
- HTTPS redirection (in production)

## License

This project is open source.
