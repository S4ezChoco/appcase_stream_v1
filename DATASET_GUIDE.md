# Dataset Guide for Flood Monitoring System

This guide explains how to prepare and manage datasets for training the flood detection model.

## Dataset Structure

```
datasets/
├── flood_dataset.yaml      # Dataset configuration
├── images/                 # All images
├── labels/                 # All labels
├── train/                  # Training data
│   ├── images/            # Training images
│   └── labels/            # Training labels
└── val/                   # Validation data
    ├── images/            # Validation images
    └── labels/            # Validation labels
```

## Dataset Configuration (flood_dataset.yaml)

```yaml
path: ./datasets
train: train/images
val: val/images

names:
  0: flood
  1: no_flood
```

## Image Requirements

- **Format**: JPG, JPEG, PNG
- **Resolution**: Minimum 640x640 pixels (recommended)
- **Quality**: High quality, clear visibility
- **Lighting**: Various lighting conditions
- **Angles**: Multiple camera angles and perspectives

## Label Format (YOLO)

Each image should have a corresponding `.txt` file with the same name containing:

```
class_id center_x center_y width height
```

Where:
- `class_id`: 0 for flood, 1 for no_flood
- `center_x`, `center_y`: Center coordinates (normalized 0-1)
- `width`, `height`: Bounding box dimensions (normalized 0-1)

## Data Collection Guidelines

### Flood Images
- Flooded streets and roads
- Overflowing rivers and streams
- Submerged areas and infrastructure
- Water accumulation in urban areas
- Various flood severity levels

### No-Flood Images
- Normal water levels in rivers
- Dry streets and roads
- Regular weather conditions
- Clear drainage systems
- Normal urban and rural scenes

## Dataset Management

### Adding New Data
1. Place images in appropriate folders
2. Create corresponding label files
3. Update the dataset configuration
4. Verify data integrity

### Data Splitting
- **Training**: 70-80% of total data
- **Validation**: 20-30% of total data
- Ensure balanced class distribution

### Quality Control
- Review all annotations for accuracy
- Remove blurry or unclear images
- Ensure consistent labeling standards
- Validate bounding box coordinates

## Training Tips

1. **Balanced Dataset**: Maintain roughly equal numbers of flood/no-flood samples
2. **Data Augmentation**: Use rotation, scaling, brightness adjustments
3. **Diverse Scenarios**: Include various weather conditions and environments
4. **Regular Updates**: Continuously add new data based on model performance

## Using the Dataset Manager

The web interface provides tools for:
- Uploading new images
- Annotating bounding boxes
- Managing dataset splits
- Validating annotations
- Exporting datasets

## Best Practices

1. **Consistent Naming**: Use descriptive, consistent file names
2. **Metadata**: Keep track of image sources and conditions
3. **Version Control**: Maintain dataset versions for reproducibility
4. **Regular Backup**: Keep backups of annotated datasets
5. **Documentation**: Document any special cases or considerations
