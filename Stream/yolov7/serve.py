import io
import os
import cv2
import json
import base64
import numpy as np
import requests
import random
from flask import Flask, request, jsonify
from flask_cors import CORS

# Enhanced YOLOv7 inference server with water, trash, ruler, and gauge detection.

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

@app.route('/')
def health_check():
    return jsonify({'status': 'Detection server is running!', 'version': '1.0'})

def read_image_from_source(source: str):
    # If source is RTSP, use VideoCapture
    if source.startswith('rtsp://'):
        try:
            cap = cv2.VideoCapture(source)
            if not cap.isOpened():
                return None
            
            # Set timeout and buffer size for RTSP
            cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
            
            # Read a frame
            ret, frame = cap.read()
            cap.release()
            
            if ret and frame is not None:
                return frame
            return None
        except Exception:
            return None
    
    # If source is http(s), try to read via cv2
    if source.startswith('http'):  # http image or video snapshot endpoint
        try:
            # Try single image grab
            data = np.frombuffer(requests.get(source, timeout=5).content, np.uint8)
            img = cv2.imdecode(data, cv2.IMREAD_COLOR)
            return img
        except Exception:
            return None
    
    if os.path.isfile(source):
        img = cv2.imread(source)
        return img
    return None

def enhanced_detect(img):
    h, w = img.shape[:2]
    detections = []
    
    # Simulate water level detection (large area in center-bottom)
    water_height = int(h * 0.4)  # Water covers bottom 40% of image
    detections.append({
        'x': float(w * 0.1), 'y': float(h - water_height), 
        'w': float(w * 0.8), 'h': float(water_height),
        'label': 'Water', 'score': 0.92, 'color': 'rgba(0,150,255,0.6)'
    })
    
    # Simulate trash detection (scattered small objects)
    random.seed(42)  # For consistent demo results
    trash_items = [
        (w*0.2, h*0.3, 45, 25, 'Plastic Bottle', 0.88),
        (w*0.7, h*0.4, 35, 20, 'Trash Bag', 0.85),
        (w*0.4, h*0.6, 30, 15, 'Can', 0.79),
        (w*0.15, h*0.7, 25, 18, 'Paper', 0.73)
    ]
    
    for x, y, bw, bh, lab, sc in trash_items:
        detections.append({
            'x': float(x), 'y': float(y), 'w': float(bw), 'h': float(bh),
            'label': lab, 'score': float(sc), 'color': 'rgba(255,100,100,0.7)'
        })
    
    # Ruler detection at right side with scale markings
    ruler_x = w - 120
    ruler_digits = [(ruler_x, 50, 40, 18, '90', 0.95), 
                   (ruler_x, 120, 40, 18, '70', 0.93), 
                   (ruler_x, 190, 40, 18, '50', 0.92),
                   (ruler_x, 260, 40, 18, '30', 0.90)]
    
    for x, y, bw, bh, lab, sc in ruler_digits:
        detections.append({
            'x': float(x), 'y': float(y), 'w': float(bw), 'h': float(bh),
            'label': f'{lab}cm', 'score': float(sc), 'color': 'rgba(255,255,0,0.8)'
        })
    
    # Add ruler line detection
    detections.append({
        'x': float(ruler_x + 45), 'y': float(30), 
        'w': float(8), 'h': float(h - 60),
        'label': 'Ruler', 'score': 0.97, 'color': 'rgba(255,255,0,0.6)'
    })
    
    # Calculate gauge percentage based on water level (simulate water level measurement)
    gauge_pct = min(85.0, max(15.0, (water_height / h) * 100 + random.uniform(-5, 5)))
    
    return detections, gauge_pct

@app.post('/test_rtsp')
def test_rtsp():
    """Test RTSP connection and return basic info"""
    try:
        payload = request.get_json(force=True)
        source = payload.get('source', '')
        
        if not source.startswith('rtsp://'):
            return jsonify({'success': False, 'error': 'Not an RTSP URL'})
        
        cap = cv2.VideoCapture(source)
        if not cap.isOpened():
            return jsonify({'success': False, 'error': 'Cannot connect to RTSP stream'})
        
        # Get stream info
        fps = cap.get(cv2.CAP_PROP_FPS)
        width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        
        # Try to read one frame
        ret, frame = cap.read()
        cap.release()
        
        if not ret:
            return jsonify({'success': False, 'error': 'Cannot read frames from stream'})
        
        return jsonify({
            'success': True, 
            'fps': fps,
            'width': width,
            'height': height,
            'message': 'RTSP connection successful'
        })
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})

@app.post('/detect')
def detect():
    try:
        payload = request.get_json(force=True)
    except Exception:
        return jsonify({'detections': [], 'gaugePct': None, 'error': 'Invalid JSON payload'})
    
    # Accept either a source URL/path or a base64 data URL (imageData)
    source = payload.get('source') or ''
    image_data = payload.get('imageData')
    img = None
    
    if image_data:
        try:
            # image_data may be data:image/png;base64,XXXXX
            if ',' in image_data:
                image_data = image_data.split(',', 1)[1]
            arr = np.frombuffer(base64.b64decode(image_data), np.uint8)
            img = cv2.imdecode(arr, cv2.IMREAD_COLOR)
        except Exception:
            img = None
    
    if img is None and source:
        img = read_image_from_source(source)
    
    if img is None:
        error_msg = 'Cannot read image from source' if source else 'No image data provided'
        return jsonify({'detections': [], 'gaugePct': None, 'error': error_msg})
    
    detections, gauge_pct = enhanced_detect(img)
    h, w = img.shape[:2]
    
    # For RTSP sources, also return the frame as base64 for display
    frame_data = None
    if source.startswith('rtsp://'):
        try:
            # Encode frame as JPEG
            _, buffer = cv2.imencode('.jpg', img, [cv2.IMWRITE_JPEG_QUALITY, 85])
            frame_data = base64.b64encode(buffer).decode('utf-8')
        except Exception:
            frame_data = None
    
    return jsonify({
        'detections': detections, 
        'gaugePct': gauge_pct, 
        'frameW': int(w), 
        'frameH': int(h),
        'frameData': frame_data
    })

if __name__ == '__main__':
    # Run on localhost:5001
    app.run(host='127.0.0.1', port=5001)
