# Flood Monitoring System

A real-time flood monitoring system with AI-powered detection capabilities using React frontend and FastAPI backend.

## Features

- Real-time camera stream monitoring
- AI-powered flood detection using YOLOv8
- Interactive dashboard with analytics
- Dataset management for model training
- WebSocket-based live updates
- Camera management system

## Project Structure

```
flood-monitoring-system/
├── backend/                 # FastAPI backend
│   ├── main.py             # Main application entry point
│   ├── ai_detector.py      # AI detection logic
│   ├── camera_manager.py   # Camera management
│   ├── database.py         # Database operations
│   ├── data_processor.py   # Data processing utilities
│   ├── dataset_manager.py  # Dataset management
│   ├── requirements.txt    # Python dependencies
│   └── routers/           # API route handlers
├── frontend/               # React frontend
│   ├── src/               # Source code
│   ├── public/            # Public assets
│   └── package.json       # Node.js dependencies
└── README.md              # This file
```

## Setup Instructions

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Install Python dependencies:
   ```bash
   pip install -r requirements.txt
   ```

3. Run the backend server:
   ```bash
   python main.py
   ```

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install Node.js dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm start
   ```

## Quick Start

Use the provided start scripts:

- **Windows**: Run `start.bat`
- **Linux/Mac**: Run `start.sh`

## API Documentation

Once the backend is running, visit `http://localhost:8000/docs` for interactive API documentation.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.
