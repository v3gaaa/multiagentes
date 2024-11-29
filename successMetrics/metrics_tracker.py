import matplotlib.pyplot as plt
import os
from datetime import datetime
import numpy as np


class SuccessMetricsTracker:
    def __init__(self, name="Security system"):
        self.name = name
        self.metrics_history = {
            'total_alerts': [],
            'true_positives': [],
            'false_positives': [],
            'detection_accuracy': [],
            'investigation_efficiency': [],
            'timestamps': []
        }
        self.metrics = {
            'total_alerts': 0,
            'true_positives': 0,
            'false_positives': 0,
            'detection_accuracy': 0.0,
            'investigation_efficiency': 0.0,
            'investigation_count': 0,
            'successful_investigations': 0
        }


    def record_detection(self, is_true_positive, timestamp=None):
        """Record a detection and update accuracy metrics"""
        
        self.metrics['total_alerts'] += 1
        if is_true_positive:
            self.metrics['true_positives'] += 1
        else:
            self.metrics['false_positives'] += 1
        
        self.metrics['detection_accuracy'] = (
            self.metrics['true_positives'] / self.metrics['total_alerts']
        ) if self.metrics['total_alerts'] > 0 else 0.0
        
        # Store historical data
        current_timestamp = timestamp or datetime.now()
        self.metrics_history['total_alerts'].append(self.metrics['total_alerts'])
        self.metrics_history['true_positives'].append(self.metrics['true_positives'])
        self.metrics_history['false_positives'].append(self.metrics['false_positives'])
        self.metrics_history['detection_accuracy'].append(self.metrics['detection_accuracy'])
        self.metrics_history['timestamps'].append(current_timestamp)


    def record_investigation(self, was_successful):
        """Record an investigation and update efficiency metrics"""
        self.metrics['investigation_count'] += 1
        if was_successful:
            self.metrics['successful_investigations'] += 1
        
        self.metrics['investigation_efficiency'] = (
            self.metrics['successful_investigations'] / self.metrics['investigation_count']
        ) if self.metrics['investigation_count'] > 0 else 0.0
        
        if len(self.metrics_history['investigation_efficiency']) == 0:
            self.metrics_history['investigation_efficiency'].append(
                self.metrics['investigation_efficiency']
            )
        else:
            last_efficiency = self.metrics_history['investigation_efficiency'][-1]
            if last_efficiency != self.metrics['investigation_efficiency']:
                self.metrics_history['investigation_efficiency'].append(
                    self.metrics['investigation_efficiency']
                )


    def generate_metrics_report(self, output_dir='metrics_reports'):
        """Generate comprehensive metrics visualization"""
        # Ensure output directory exists
        os.makedirs(output_dir, exist_ok=True)
        
        # Handle case with no data
        if all(len(hist) == 0 for hist in self.metrics_history.values()):
            print(f"No metrics data available for {self.name}")
            
            # Create a placeholder image
            plt.figure(figsize=(10, 6))
            plt.text(0.5, 0.5, f"No metrics data for {self.name}", 
                     horizontalalignment='center', verticalalignment='center')
            plt.axis('off')
            
            report_path = os.path.join(output_dir, f'{self.name}_no_metrics.png')
            plt.savefig(report_path)
            plt.close()
            return report_path
        
        # Ensure all lists have at least one element
        for key in self.metrics_history:
            if len(self.metrics_history[key]) == 0:
                self.metrics_history[key].append(0)
        
        # Trim to the shortest list
        min_length = min(len(hist) for hist in self.metrics_history.values())

        # Prepare data for plotting
        timestamps = self.metrics_history['timestamps'][:min_length]
        detection_accuracy = self.metrics_history['detection_accuracy'][:min_length]

        investigation_efficiency = (
            self.metrics_history['investigation_efficiency'][:min_length] 
            if self.metrics_history['investigation_efficiency'] 
            else [0] * min_length
        )

        # Create visualization
        plt.figure(figsize=(12, 8))
        plt.suptitle(f'{self.name} - Metrics overview', fontsize=16)
        
        # Detection accuracy
        plt.subplot(2, 2, 1)
        plt.plot(timestamps, detection_accuracy, marker='o', label='Detection accuracy')
        plt.title('Detection accuracy over time')
        plt.xlabel('Time')
        plt.ylabel('Accuracy')
        plt.legend()
        
        # Investigation efficiency
        plt.subplot(2, 2, 2)
        plt.plot(timestamps, investigation_efficiency, marker='o', color='green', label='Investigation efficiency')
        plt.title('Investigation efficiency over time')
        plt.xlabel('Time')
        plt.ylabel('Efficiency')
        plt.legend()

        # Total alerts
        plt.subplot(2, 2, 3)
        plt.plot(timestamps, self.metrics_history['total_alerts'][:min_length], marker='o', color='blue', label='Total Alerts')
        plt.title('Total alerts over time')
        plt.xlabel('Time')
        plt.ylabel('Number of alerts')
        plt.legend()

        # True vs False positives
        plt.subplot(2, 2, 4)
        plt.plot(timestamps, self.metrics_history['true_positives'][:min_length], marker='o', color='green', label='True Positives')
        plt.plot(timestamps, self.metrics_history['false_positives'][:min_length], marker='o', color='red', label='False Positives')
        plt.title('True vs False positives')
        plt.xlabel('Time')
        plt.ylabel('Count')
        plt.legend()
        
        plt.tight_layout()
        
        
        # Save report
        report_path = os.path.join(output_dir, f'{self.name}_metrics_report.png')
        plt.savefig(report_path)
        plt.close()
        
        return report_path


    def get_metrics(self):
        """Return current metrics"""
        return self.metrics