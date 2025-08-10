#!/bin/bash

echo "Starting Flood Monitoring System..."
echo

echo "Starting Backend Server..."
cd backend
python main.py &
BACKEND_PID=$!

echo "Starting Frontend Development Server..."
cd ../frontend
npm start &
FRONTEND_PID=$!

echo
echo "Both servers are starting..."
echo "Backend: http://localhost:8000"
echo "Frontend: http://localhost:3000"
echo
echo "Press Ctrl+C to stop both servers"

# Function to cleanup processes on exit
cleanup() {
    echo
    echo "Stopping servers..."
    kill $BACKEND_PID 2>/dev/null
    kill $FRONTEND_PID 2>/dev/null
    exit 0
}

# Trap Ctrl+C
trap cleanup INT

# Wait for processes
wait
