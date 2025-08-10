@echo off
echo Starting Flood Monitoring System...
echo.

echo Starting Backend Server...
cd backend
start cmd /k "python main.py"

echo Starting Frontend Development Server...
cd ../frontend
start cmd /k "npm start"

echo.
echo Both servers are starting...
echo Backend: http://localhost:8000
echo Frontend: http://localhost:3000
echo.
echo Press any key to exit this window...
pause > nul
