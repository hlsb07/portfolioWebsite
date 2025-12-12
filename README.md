# Portfolio Website with GDPR-Compliant Analytics

A professional portfolio website with integrated, GDPR-compliant analytics tracking. The project consists of a static frontend served by Nginx and a backend API built with ASP.NET Core and PostgreSQL.

## Features

### Frontend
- 📱 Responsive, modern portfolio website
- 🎨 Light/dark theme toggle
- ♿ Accessibility-first design (ARIA labels, skip links)
- 🚀 Optimized performance
- 📊 Anonymous analytics tracking (GDPR-compliant)

### Backend Analytics
- ✅ **100% GDPR-Compliant**
  - No cookies
  - No IP address storage
  - No browser fingerprinting
  - Anonymous visitor identification using SHA-256 hashing
- 📈 Tracks:
  - Page visits and session duration
  - Scroll depth (25%, 50%, 75%, 100%)
  - Section viewing time
  - Device and browser statistics (anonymized)
- 🔐 Password-protected analytics dashboard
- 🐳 Docker-ready deployment

## Project Structure

```
JobApplication/
├── frontend/
│   ├── public/
│   │   ├── index.html          # Main portfolio page
│   │   ├── styles.css          # Styling
│   │   ├── script.js           # Portfolio interactions
│   │   ├── analytics.js        # GDPR-compliant tracking
│   │   ├── dashboard.html      # Analytics dashboard
│   │   └── images/             # Static assets
│   ├── nginx.conf              # Nginx configuration
│   └── Dockerfile              # Frontend container
├── backend/
│   ├── PortfolioAnalytics/
│   │   ├── Models/             # Database entities
│   │   ├── Data/               # EF Core DbContext
│   │   ├── Controllers/        # API endpoints
│   │   ├── DTOs/               # Data transfer objects
│   │   ├── Services/           # Business logic
│   │   ├── Program.cs          # Application entry point
│   │   └── appsettings.json    # Configuration
│   ├── Dockerfile              # Backend container
│   └── .dockerignore
├── docker-compose.yml          # Orchestration
├── .env.example                # Environment variables template
├── .gitignore
└── README.md                   # This file
```

## Technology Stack

### Frontend
- HTML5, CSS3, JavaScript (ES6+)
- Nginx (Alpine)
- Modern CSS (Grid, Flexbox, Variables)
- Vanilla JavaScript (no frameworks)

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL 16
- Docker & Docker Compose

### Analytics
- SHA-256 hashing for anonymous visitor IDs
- UAParser for device detection
- GDPR-compliant tracking

## Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET SDK 8.0 (for local development)
- PostgreSQL (for local development without Docker)

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   cd JobApplication
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   ```

3. **Edit `.env` file** and set your secrets:
   ```env
   POSTGRES_PASSWORD=your_secure_password
   ANALYTICS_SERVER_SECRET=your_random_secret_string
   ANALYTICS_PASSWORD=your_dashboard_password
   ```

4. **Start all services**
   ```bash
   docker-compose up -d
   ```

5. **Access the application**
   - Frontend: http://localhost:8080
   - Analytics Dashboard: http://localhost:8080/dashboard.html
   - Backend API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger (Development only)

### Local Development (Without Docker)

#### Backend Setup

1. **Navigate to backend directory**
   ```bash
   cd backend/PortfolioAnalytics
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Update connection string** in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=portfolio_analytics;Username=postgres;Password=postgres"
     }
   }
   ```

4. **Run the backend**
   ```bash
   dotnet run
   ```

#### Frontend Setup

1. **Open `frontend/public/index.html`** in a browser
2. Or use a simple HTTP server:
   ```bash
   cd frontend/public
   python -m http.server 8080
   ```

## API Endpoints

### Analytics Tracking

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/analytics/visit` | Track page visit |
| POST | `/api/analytics/scroll` | Track scroll depth |
| POST | `/api/analytics/section` | Track section viewing |
| POST | `/api/analytics/end` | End visit session |
| GET | `/api/analytics/stats?password=xxx` | Get statistics (password-protected) |
| GET | `/health` | Health check |

### Example Request

```javascript
// Track a visit
fetch('http://localhost:5000/api/analytics/visit', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    anonymousIdHash: '7a8f3b2c1d...',
    page: '/',
    referrer: 'https://google.com',
    userAgent: 'Mozilla/5.0...'
  })
});
```

## Database Schema

```sql
Visitors
  - Id (PK)
  - AnonymousIdHash (unique, SHA-256)
  - FirstSeen
  - LastSeen

Visits
  - Id (PK)
  - VisitorId (FK)
  - Page
  - Timestamp
  - DurationMs
  - Referrer

ScrollEvents
  - Id (PK)
  - VisitId (FK)
  - ScrollDepthPercent (25, 50, 75, 100)
  - Timestamp

SectionEvents
  - Id (PK)
  - VisitId (FK)
  - SectionName
  - DurationMs
  - Timestamp

DeviceInfos
  - Id (PK)
  - VisitorId (FK)
  - BrowserFamily
  - BrowserVersion
  - OSFamily
  - OSVersion
  - DeviceType
  - FirstSeen
```

## GDPR Compliance

This analytics system is designed to be fully GDPR-compliant:

### What We Track
- Anonymous visitor IDs (SHA-256 hash of User-Agent + server secret)
- Page views and navigation patterns
- Scroll depth and section engagement
- Browser and device types (general categories only)
- Session duration

### What We DON'T Track
- ❌ Cookies
- ❌ IP addresses
- ❌ Personally identifiable information
- ❌ Cross-site tracking
- ❌ Unique device fingerprints

### How It Works
The anonymous visitor ID is generated using:
```
SHA-256(UserAgent + ServerSecret)
```

This creates a non-reversible hash that:
- Allows recognizing return visitors
- Cannot identify individual users
- Cannot be traced back to a person
- Complies with GDPR Article 6(1)(f) - legitimate interest

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_PASSWORD` | PostgreSQL password | `change_me_in_production` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | `Production` |
| `ANALYTICS_SERVER_SECRET` | Secret for visitor hash | `CHANGE_THIS_TO_A_RANDOM_SECRET` |
| `ANALYTICS_PASSWORD` | Dashboard password | `change-me-in-production` |

### CORS Configuration

The backend automatically configures CORS based on environment:

- **Development**: Allows all origins
- **Production**: Only allows:
  - `https://app.jan-huelsbrink.de`
  - `https://portfolio.jan-huelsbrink.de`
  - `http://app.jan-huelsbrink.de`
  - `http://portfolio.jan-huelsbrink.de`

Update [Program.cs:40-44](backend/PortfolioAnalytics/Program.cs#L40-L44) to change production domains.

## Deployment

### Docker Deployment (Recommended)

1. **Build and start services**
   ```bash
   docker-compose up -d --build
   ```

2. **View logs**
   ```bash
   docker-compose logs -f
   ```

3. **Stop services**
   ```bash
   docker-compose down
   ```

4. **Stop and remove volumes** (⚠️ This deletes database data!)
   ```bash
   docker-compose down -v
   ```

### Production Deployment

1. **Update environment variables** in `.env`
2. **Set strong passwords and secrets**
3. **Update CORS domains** in [Program.cs](backend/PortfolioAnalytics/Program.cs)
4. **Update API URLs** in:
   - [analytics.js:16-18](frontend/public/analytics.js#L16-L18)
   - [dashboard.html](frontend/public/dashboard.html)
5. **Deploy using Docker Compose** or container orchestration (Kubernetes, etc.)

### Health Checks

All services include health checks:
- Frontend: `http://localhost:8080/health`
- Backend: `http://localhost:5000/health`
- Database: PostgreSQL `pg_isready`

## Development

### Running Tests
```bash
cd backend/PortfolioAnalytics
dotnet test
```

### Database Migrations

Create a new migration:
```bash
cd backend/PortfolioAnalytics
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

### Viewing Analytics

1. Navigate to http://localhost:8080/dashboard.html
2. Enter the dashboard password (from `ANALYTICS_PASSWORD` env variable)
3. View real-time statistics

## Troubleshooting

### Backend won't start
- Check PostgreSQL is running: `docker-compose ps`
- Check connection string in environment variables
- View logs: `docker-compose logs backend`

### Analytics not tracking
- Check browser console for errors
- Verify backend is accessible: `curl http://localhost:5000/health`
- Check CORS configuration

### Database connection errors
- Verify PostgreSQL is healthy: `docker-compose ps postgres`
- Check credentials in `.env` file
- Ensure database has been created

## Security Considerations

1. **Change default passwords** in production
2. **Use strong secrets** for `ANALYTICS_SERVER_SECRET`
3. **Enable HTTPS** in production (use reverse proxy like Nginx or Traefik)
4. **Regularly update** Docker images and .NET packages
5. **Monitor logs** for suspicious activity
6. **Backup database** regularly

## License

This project is for portfolio purposes.

## Author

Jan Hülsbrink
- Portfolio: https://portfolio.jan-huelsbrink.de
- GitHub: https://github.com/JanHuelsbrink

## Acknowledgments

- GDPR-compliant analytics inspired by Plausible Analytics
- UAParser library for device detection
- ASP.NET Core and Entity Framework Core teams
