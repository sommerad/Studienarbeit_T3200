#pragma once
#include "cloud_viewer.h"
#include <mutex>

struct ObjectPose 
{
    Eigen::Vector3f position; // Mittelpunkt (X, Y, Z)
    Eigen::Matrix3f orientation; // Rotationsmatrix
    int type; // 0: Ebene, 1: Zylinder 2:Kreis (Zylinderstehend)
};

struct Point2D
{
	//Kommt von der 2D Erkennung
    float x;
    float y;
    float radius;
    float winkel;
    int objektType; // 0 = plane, 1 = cylinder, 2 = circle, 3 = square
};


class Prozessing : public CloudViewer
{

private:
    void ApplyROI(pcl::PointCloud<pcl::PointXYZ>::Ptr inputCloud, float xMin, float xMax, float yMin, float yMax);
    ObjectPose EstimateObjectPose(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud, float x_center, float y_center, float radius,int objektType,float angle);
    pcl::PointCloud<pcl::PointXYZ>::Ptr ScalePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud, float scaleFactor);
    void FitParabolicCylinder(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud, pcl::ModelCoefficients::Ptr& cylinder_coeffs, pcl::PointIndices::Ptr& cylinder_inliers);
    Eigen::Matrix3f RodriguesToRotationMatrix(const Eigen::Vector3f& rvec);
    pcl::PointCloud<pcl::PointXYZ>::Ptr TransformCoordinateSystem(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud);
    Eigen::Matrix4f LoadCalibrationData(const char* pFileName);
   
    
	int objectCount=0;
   



public:
    int ProcessPoints(Point2D* inputPoints, int inputCount, ObjectPose* outputPoses, int* outputCount);
};
