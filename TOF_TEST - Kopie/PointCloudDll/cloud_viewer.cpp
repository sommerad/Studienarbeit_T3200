//--------------------------------Includes----------------------------//
#include "cloud_viewer.h"


//--------------------Konstruktor und Destruktor-----------------------//
CloudViewer::CloudViewer() : aIsVisualizerRunning(false), aIsNewCloudReady(false), aNewCloud(nullptr)
{
    this->aViewport1 = 1;
    this->aViewport2 = 2;
    this->aZPlane = 0.0f;
    this->aParameters = Parameters();
    this->aOrginalCloud = pcl::PointCloud<pcl::PointXYZ>::Ptr(new pcl::PointCloud<pcl::PointXYZ>());
    this->aColorCloud = pcl::PointCloud<pcl::PointXYZRGB>::Ptr(new pcl::PointCloud<pcl::PointXYZRGB>());
    this->aNewCloud = pcl::PointCloud<pcl::PointXYZRGB>::Ptr(new pcl::PointCloud<pcl::PointXYZRGB>());
    this->aTransformMatrix = Eigen::Matrix4f::Identity();
    this->aUndergroundCoeffs = pcl::ModelCoefficients::Ptr(new pcl::ModelCoefficients);
}
CloudViewer::~CloudViewer()
{
    StopVisualizerThread();
    this->aNewCloud.reset();
    this->aOrginalCloud.reset();
    this->aColorCloud.reset();
    this->aUndergroundCoeffs.reset();


}



pcl::PointCloud<pcl::PointXYZ>::Ptr CloudViewer::CalibratePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{
    /*ANLEITUNG:
    * Hier wird die Z-Ebene der Punktwolke auf 0 gesetzt. Das wird gemacht um die Punktwolke rechtwinklig zur 2D-Kamera zu setzen.
    * Die Punktwolke kann jenach Montage der Kameras schief sein. Durch die Kalibrierung der Punktwolke hat man einen ebenen Hintergrund und somit eine ebene
    * Z-Ebene. Dadurch wird sichergestellt das die Höhenmessung auf dem gesamten Bereich gleich ist.
    * Zusätzlich wird die Punktwolke um den Parameter rotationAroungZDeg gedreht.
    *
    * Als erstes wird mit RANSAC eine Ebene geschätzt.Dann wird die Ebene geneigt um konstant auf z=0 zu sein.
    */
    pcl::PointIndices::Ptr inliers(new pcl::PointIndices);
    pcl::ModelCoefficients::Ptr coefficients(new pcl::ModelCoefficients);
    pcl::SACSegmentation<pcl::PointXYZ> seg;
    seg.setOptimizeCoefficients(true);
    seg.setModelType(pcl::SACMODEL_PLANE);
    seg.setMethodType(pcl::SAC_RANSAC);
    seg.setDistanceThreshold(this->aParameters.calibrationThresholdBackground);

    seg.setInputCloud(cloud);
    seg.segment(*inliers, *coefficients);

    if (inliers->indices.empty())
    {
        throw(2);
    }

    // Extrahiere die Ebenenparameter
    float a = coefficients->values[0];
    float b = coefficients->values[1];
    float c = coefficients->values[2];
    float d = coefficients->values[3];

    // Normale der Ebene und Zielrichtung (Z-Achse)
    Eigen::Vector3f plane_normal(a, b, c);
    Eigen::Vector3f up_vector(0.0, 0.0, 1.0); // Zielrichtung (Z-Achse)

    // Rotationsmatrix berechnen
    Eigen::Quaternionf rotation;
    rotation.setFromTwoVectors(plane_normal, up_vector);

    // Translation berechnen (Verschiebung der Ebene auf z=0)
    float distance_to_origin = d / plane_normal.norm(); // Abstand der Ebene vom Ursprung
    if (this->aParameters.selectedCamera == 2)
    {
		distance_to_origin = -distance_to_origin;
    }
    Eigen::Vector3f translation(0, 0, distance_to_origin);

    // Transformation zusammenstellen
    Eigen::Affine3f transform = Eigen::Affine3f::Identity();
    transform.linear() = rotation.toRotationMatrix();
    transform.translation() = translation;


    float z_rotation_angle_rad = this->aParameters.rotationAroungZDeg * M_PI / 180.0f; // Grad zu Radiant
    Eigen::Matrix3f z_rotation;
    z_rotation = Eigen::AngleAxisf(z_rotation_angle_rad, Eigen::Vector3f::UnitZ());

    // Kombination der Transformationen
    transform.linear() = z_rotation * transform.linear();

    // Punktwolke transformieren
    pcl::PointCloud<pcl::PointXYZ>::Ptr calibrated_cloud(new pcl::PointCloud<pcl::PointXYZ>);
    pcl::transformPointCloud(*cloud, *calibrated_cloud, transform);

    inliers.reset();
    coefficients.reset();

    return calibrated_cloud;
}



pcl::PointCloud<pcl::PointXYZ>::Ptr CloudViewer::RemoveGroundByZHistogram(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{
    /*ANLEITUNG:
    * Da sich die Koeffizienten der Z-Ebene geändert haben, wird die Z-Ebene neu geschätzt. Diesmal ist es jedoch die richtige Ebene,
    * weshalb die Daten der Ebene gespeichert werden.
    * Alle Punkte die vom Algorithmus als Bodenpunkte erkannt werden, werden entfernt.
    * Alle anderen werden in die neue Punktwolke gespeichert. Somit wird der Hintergrund entfernt, was übersichtlicher ist und Rechenleistung spart
    */

    pcl::PointIndices::Ptr inliers(new pcl::PointIndices);
    pcl::SACSegmentation<pcl::PointXYZ> seg;

    // Ebene mit RANSAC schätzen
    seg.setOptimizeCoefficients(true);
    seg.setModelType(pcl::SACMODEL_PLANE);
    seg.setMethodType(pcl::SAC_RANSAC);
    seg.setDistanceThreshold(this->aParameters.backgroundRemoveThresshold);
    seg.setInputCloud(cloud);
    seg.segment(*inliers, *this->aUndergroundCoeffs);

    if (inliers->indices.empty())
    {
        throw(3);
    }

    // Ebene: Ax + By + Cz + D = 0 (Modell aus RANSAC)
    float A = this->aUndergroundCoeffs->values[0];
    float B = this->aUndergroundCoeffs->values[1];
    float C = this->aUndergroundCoeffs->values[2];
    float D = this->aUndergroundCoeffs->values[3];

    pcl::PointCloud<pcl::PointXYZ>::Ptr cloud_filtered(new pcl::PointCloud<pcl::PointXYZ>);
    float zPlane = 0;

    for (const auto& point : cloud->points)
    {
        if (this->aParameters.zMaxROI > 10000)
        {
            cloud_filtered->points.push_back(point);
			continue;
        }
        // Um Z zu berechnen: Z = -(Ax + By + D) / C
        zPlane = -(A * point.x + B * point.y + D) / C;

        // Wenn der Punkt unter der Ebene liegt, entferne ihn
        if (point.z > zPlane - this->aParameters.backgroundRemoveThresshold)
        {
            continue;
        }
        if (point.z < this->aParameters.zMaxROI)
        {
            continue;
        }
        
        cloud_filtered->points.push_back(point);
    }
    this->aZPlane = zPlane - this->aParameters.backgroundRemoveThresshold;

    cloud_filtered->width = cloud_filtered->points.size();
    cloud_filtered->height = 1;
    cloud_filtered->is_dense = cloud->is_dense;

    inliers.reset();
    return cloud_filtered;
}



//---------------------------VISULIZE PLANE-----------------------------//
pcl::PointCloud<pcl::PointXYZRGB>::Ptr CloudViewer::ColorizePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{
    /*ANLEITUNG:
    * Zur besseren Visualisierung wird die Punktwolke eingefärbt. Die Farbe der Punkte hängt von der Entfernung ab.
    * Die Punkte die näher sind, sind blau und die Punkte die weiter sind, sind rot.
    * Die Entfernung wird durch die Parameter vorgegeben.
    *
    * Es wird immer nur der Z-Wert betrachtet auf welche Höhe der Punkt ist abhänging von der Z-Ebene.
    */
    pcl::PointCloud<pcl::PointXYZRGB>::Ptr coloredCloud = pcl::PointCloud<pcl::PointXYZRGB>::Ptr(new pcl::PointCloud<pcl::PointXYZRGB>());

    for (const auto& point : cloud->points)
    {
        pcl::PointXYZRGB coloredPoint;
        coloredPoint.x = point.x;
        coloredPoint.y = point.y;
        coloredPoint.z = point.z;

        float normalizedDistance;

        if (point.z < this->aParameters.minDistanceMeasure)
        {
            normalizedDistance = 0.0f;
        }
        else if (point.z > this->aParameters.maxDistanceMeasure)
        {
            normalizedDistance = 1.0f;
        }
        else
        {
            // Normalisierung zwischen minDistance und maxDistance
            normalizedDistance = (point.z - this->aParameters.minDistanceMeasure) / (this->aParameters.maxDistanceMeasure - this->aParameters.minDistanceMeasure);
        }

        uint8_t r, g, b;

        // Rot steigt mit der Entfernung
        if (normalizedDistance <= 0.5f)
        {
            // Übergang von Blau nach Grün
            float factor = normalizedDistance / 0.5f;
            r = 0;
            g = static_cast<uint8_t>(factor * 255);
            b = static_cast<uint8_t>((1.0f - factor) * 255);
        }
        else
        {
            // Übergang von Grün nach Rot
            float factor = (normalizedDistance - 0.5f) / 0.5f;
            r = static_cast<uint8_t>(factor * 255);
            g = static_cast<uint8_t>((1.0f - factor) * 255);
            b = 0;
        }

        coloredPoint.r = r;
        coloredPoint.g = g;
        coloredPoint.b = b;
        //Neue Punktwolke wird erstellt
        coloredCloud->points.push_back(coloredPoint);
    }

    coloredCloud->width = cloud->width;
    coloredCloud->height = cloud->height;
    coloredCloud->is_dense = cloud->is_dense;
    cloud.reset();
    return coloredCloud;
}
void CloudViewer::UpdatePointCloud(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud)
{
    /*ANLEITUNG:
    * Mutex sperrt den Thread um Race Conditions zu vermeiden ->Wegen verschieden Threads kann es sein das sie auf die gleiche Variable zugreifen
    * Das Problem ist das die Visualisierungsfunktion in einem anderen Thread läuft und die Punktwolke in einem anderen Thread aktualisiert wird.
    * Somit muss es gesperrt werden
    * Lock_guard ist ein RAII-Wrapper, der den Mutex sperrt, wenn er erstellt wird, und ihn freigibt, wenn er zerstört wird.
    * Somit wird die Sperrung automatisch erledigt und man muss es nicht aktiv sperren und freigeben
    */

	this->aVisualizerMutex.lock();
	if (this->aNewCloud!=nullptr)
	{
		this->aNewCloud->clear();
		this->aNewCloud.reset();
	}
    this->aNewCloud = cloud;
    this->aIsNewCloudReady = true;
	this->aVisualizerMutex.unlock();
}
void CloudViewer::PointPickingCallback(const pcl::visualization::PointPickingEvent& event, void* viewer_void)
{
    /*ANLEITUNG:
    * Sobald man mit Shift(Links) und einem Links Klick auf einen Punkt klicke, wird der Punkt ausgewählt und diese Funktion aufgerufen
    * Mit dieser Funktion wird dann der Punkt ausgewählt und die Koordinaten des Punktes werden angezeigt
    * Dies Hilft zur besseren Visualisierung um sich in der Punktwolke zu orientieren
    */

    pcl::visualization::PCLVisualizer* viewer = static_cast<pcl::visualization::PCLVisualizer*>(viewer_void);
    this->aPCLVisualizer->removeText3D("SelectedPoint");
    float x, y, z;
    event.getPoint(x, y, z);

    if (event.getPointIndex() == -1)
    {
        return;
    }
    else
    {
        std::string selectedPoint = "X: " + std::to_string((int)x) + " Y: " + std::to_string((int)y) + " Z: " + std::to_string((int)z);
        this->aPCLVisualizer->addText3D(selectedPoint, pcl::PointXYZ(x, y, z), this->aParameters.textScale, 1.0, 1.0, 1.0, "SelectedPoint");
    }
}
void CloudViewer::StartVisualizerThread(Parameters pParameter)
{
    /*ANLEITUNG:
    * Der Thread wird sofort gestartet, sobald das C# Objekt erstellt wird.
    * Dann läuft die Visualisierungsfunktion in einem eigenen Thread und aktualisiert die Punktwolke.
    * Die Funktion überprüft ob die Punktwolke aktualisiert wurde und aktualisiert die Visualisierung.
    *
    * Es werden zunächst die Parameter für den Visulizer erstellt und dann in einer Dauerschleife die Punktwolke aktualisiert.
    *
    * Mit dem Parameter twoViewports kann man zwei Ansichten der Punktwolke sehen.
    */
    this->aParameters = pParameter;
    this->aROILowerLimit = Eigen::Vector4f(this->aParameters.xMinROI, this->aParameters.yMinROI, this->aParameters.zMinROI, 1.0);
    this->aROIUpperLimit = Eigen::Vector4f(this->aParameters.xMaxROI, this->aParameters.yMaxROI, this->aParameters.zMaxROI, 1.0);
    this->aIsVisualizerRunning = true;

    this->aPCLVisualizer = boost::make_shared<pcl::visualization::PCLVisualizer>("3D Viewer");

    this->aPCLVisualizer->removeCoordinateSystem();
    this->aPCLVisualizer->addCoordinateSystem(this->aParameters.coorSystemScale,"reference");
    this->aPCLVisualizer->initCameraParameters();
    this->aPCLVisualizer->setSize(this->aParameters.windowWidth, this->aParameters.windowHeight);
    this->aPCLVisualizer->registerPointPickingCallback(&CloudViewer::PointPickingCallback, *this);
    //std::thread workerThread(&CloudViewer::Visualize, this);


    if (this->aParameters.twoViewports)
    {

        this->aPCLVisualizer->createViewPort(0.0, 0.0, 0.5, 1.0, this->aViewport1); // Linker Viewport
        this->aPCLVisualizer->createViewPort(0.5, 0.0, 1.0, 1.0, this->aViewport2); // Rechter Viewport

        this->aPCLVisualizer->setBackgroundColor(0.1, 0.1, 0.1, this->aViewport1);
        this->aPCLVisualizer->setBackgroundColor(0.2, 0.2, 0.2, this->aViewport2);

        this->aPCLVisualizer->addText("Input Image", 10, 10, 12, 1.0, 1.0, 1.0, "Viewport1Text", this->aViewport1);
        this->aPCLVisualizer->addText("Mofified Image", 10, 10, 12, 1.0, 1.0, 1.0, "Viewport2Text", this->aViewport2);
    }

    //Ab hier läuft der Thread in einer Endlosschleife
    while (this->aIsVisualizerRunning)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));

        if (this->aIsNewCloudReady) //Sobald neue Punktwolke da ist werden die Ressourcen gelockt und die Wolke aktualisuert
        {
           
			this->aVisualizerMutex.lock();
           

            if (this->aParameters.twoViewports)
            {
                this->aPCLVisualizer->removeAllPointClouds(this->aViewport1);
                this->aPCLVisualizer->removeAllPointClouds(this->aViewport2);
                this->aPCLVisualizer->addPointCloud(this->aOrginalCloud, "CloudViewport1", this->aViewport1);
                this->aPCLVisualizer->addPointCloud(this->aNewCloud, "CloudViewport2", this->aViewport2);

            }
            else
            {

                this->aPCLVisualizer->removeAllPointClouds();
                this->aPCLVisualizer->addPointCloud(this->aNewCloud, "CloudViewport1");
            }

			this->aVisualizerMutex.unlock();
            this->aIsNewCloudReady = false;

        }
        std::lock_guard<std::mutex> lock(this->aVisualizerMutex);
        if (this->aPCLVisualizer) 
        {
            this->aPCLVisualizer->spinOnce(100);
        }
    }
}
void CloudViewer::Visualize()
{

}
void CloudViewer::StopVisualizerThread()
{
    this->aIsVisualizerRunning = false;

    if (this->aPCLVisualizer)
    {
        this->aPCLVisualizer->close();
        this->aPCLVisualizer.reset();
    }
}