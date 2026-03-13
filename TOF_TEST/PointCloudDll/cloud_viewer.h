#pragma once
//ANLEITUNG
/*
* Diese Datei ist Teil des PointCloudDll-Projekts.
 *
 * PointCloudDll ist ein C++-Projekt, das die Visualisierung und Verarbeitung von Punktwolken ermöglicht.
 *
 * Diese Header-Datei definiert die Klasse CloudViewer, die für die Visualisierung von Punktwolken
 * unter Verwendung der PCL (Point Cloud Library) verantwortlich ist.
 * 
 * Die Anleitung zum richtigen Installieren der PCL findet man hier:
 * https://github.com/PointCloudLibrary/pcl/issues/4462
 * 
 */ 


#include <atomic>
#include <boost_1_67_0/boost/bind.hpp>
#include <boost_1_67_0/boost/thread/mutex.hpp>
#include <boost_1_67_0/boost/thread/thread.hpp>
#include <condition_variable>
#include <fstream>
#include <iostream>
#include <memory>
#include <mutex>
#include <nlohmann/json.hpp>
#include <opencv2/core.hpp>
#include <opencv2/opencv.hpp>
#include <pcl/common/io.h>
#include <pcl/common/pca.h>
#include <pcl/common/transforms.h>
#include <pcl/features/integral_image_normal.h>
#include <pcl/features/normal_3d.h>
#include <pcl/filters/voxel_grid.h>       
#include <pcl/filters/statistical_outlier_removal.h> 
#include <pcl/filters/radius_outlier_removal.h>
#include <pcl/features/principal_curvatures.h>
#include <pcl/filters/crop_box.h>
#include <pcl/filters/extract_indices.h>
#include <pcl/io/pcd_io.h>
#include <pcl/io/ply_io.h>
#include <pcl/point_cloud.h>
#include <pcl/point_types.h>
#include <pcl/sample_consensus/method_types.h>
#include <pcl/sample_consensus/model_types.h>
#include <pcl/segmentation/progressive_morphological_filter.h>
#include <pcl/segmentation/sac_segmentation.h>
#include <pcl/visualization/cloud_viewer.h>


#include <thread>

using json = nlohmann::json;
struct Parameters
{
	//Diese Parameter werden einmalig beim Starten des Programms von der PointCloudProcessing.cs gesetzt.
	char* dataPath;
	char* calibrationPath;
    int selectedCamera;
    bool  twoViewports;
	float maxDistanceMeasure;
	float minDistanceMeasure;
	float backgroundRemoveThresshold;
    float calibrationThresholdBackground;
    float planeDetectionThreshold;
    float cylinderDetectionThreshold;
	float RANSACmaxIteration;
    float cylinderMinRadius;
    float cylinderMaxRadius;
    double textScale;
    double coorSystemScale;
    int windowWidth;
    int windowHeight;
	float xMinROI;
	float xMaxROI;
	float yMinROI;
	float yMaxROI;
	float zMinROI;
	float zMaxROI;
	float xKoordinateOffset;
	float yKoordinateOffset;
	float zKoordinateOffset;
    float rotationAroungZDeg;
	float xKoordianteSkaling;
	float yKoordianteSkaling;
	float zKoordianteSkaling;
    float vogelGridSize;
    float statisticalOutlierRemovalRadius;
    int statisticalOutlierRemovalNeighbors;
    float RadiusOutlierRemovalRadius;
    int RadiusOutlierRemovalMinNeighbors;
    float pointCloudScale;

};

class CloudViewer
{
    public:
        CloudViewer();
        ~CloudViewer();
       
        void StartVisualizerThread(Parameters pParameter);
        void Visualize();
        void StopVisualizerThread();
        
    protected:
        bool aCalibrationDataLoaded;
        int aViewport1;
        int aViewport2;
        float aZPlane;

        Parameters aParameters;
        pcl::PointCloud<pcl::PointXYZRGB>::Ptr aNewCloud;
        pcl::PointCloud<pcl::PointXYZ>::Ptr aOrginalCloud;
        pcl::PointCloud<pcl::PointXYZRGB>::Ptr aColorCloud;
        pcl::ModelCoefficients::Ptr aUndergroundCoeffs;

        Eigen::Vector4f aROIUpperLimit;
        Eigen::Vector4f aROILowerLimit;
        Eigen::Vector3f aRotationVec;
        Eigen::Vector3f aTranslationVec;
        Eigen::Matrix4f aTransformMatrix;

       
        std::mutex aVisualizerMutex;
        std::atomic<bool> aIsVisualizerRunning;
        std::atomic<bool> aIsNewCloudReady;
        boost::shared_ptr<pcl::visualization::PCLVisualizer> aPCLVisualizer;
       

        void PointPickingCallback(const pcl::visualization::PointPickingEvent& event, void* viewer_void);
        void UpdatePointCloud(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud);
        pcl::PointCloud<pcl::PointXYZRGB>::Ptr ColorizePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud);
        pcl::PointCloud<pcl::PointXYZ>::Ptr RemoveGroundByZHistogram(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud);
        pcl::PointCloud<pcl::PointXYZ>::Ptr CalibratePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud);
       
};


