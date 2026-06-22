#include "cloud_processing.h"

//--------------------Modified Undergroundplane-----------------------//
Eigen::Matrix3f Prozessing::RodriguesToRotationMatrix(const Eigen::Vector3f& rvec)
{
//    //ANLEIITUNG:
//    /* 
//    * Durch die 2D-Kamera erhalten wir zwei Vektor Kalibrierungsplatte. Mithilfe der Rodrigues-Formel können wir die Rotationmatrix berechnen.
//    * Durch diese Rotationmatrix können wir die Punktwolke kalibrieren und somit die Punktwolke rechtwinklig zur 2D-Kamera ausrichen.
//    * Dies geht nur wenn die Kameras sehr nahe bei einander sind und die Kalibrierungsplatte und die Kameras parralel zueinander sind.
//    * Sollte durch die Montage die Koordinatensystem nicht rechtwincklig zueinander sein, muss die Punktwolke durch den Parameter
//    * rotationAroungZDeg gedreht werden. Dadurch kann man die Punktwolke anpassen
//    *
//    * Die Rodrigues-Formel ist eine Methode zur Darstellung einer Drehung in der dreidimensionalen Euklidischen Geometrie durch einen Vektor.
//    * https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
//    * Wichtig ist hierbei das nur die Rotation um die Z-Achse betrachtet wird. Die Z-ebene kann an sich schief sein und wird später durch CalibratePointCloud
//    * auf eine konstante Z-Ebene gebracht.
//    * 
//	* Diese Funktion wurde nur bei der Schmersal Kamera verwendet. Bei der Sick-Kamera schlägt die Berechnung fehl.
//    */
//
    float theta = rvec.norm();
    Eigen::Matrix3f rotationMatrix = Eigen::Matrix3f::Identity();
    if (theta > 1e-6)
    {
        Eigen::Vector3f k = rvec.normalized();
        Eigen::Matrix3f K;
        K << 0, -k(2), k(1),
            k(2), 0, -k(0),
            -k(1), k(0), 0;

        rotationMatrix = Eigen::Matrix3f::Identity() + (std::sin(theta) * K + (1 - std::cos(theta)) * K * K);
    }

    return rotationMatrix;
}


Eigen::Matrix4f Prozessing::LoadCalibrationData(const char* pFileName)
{
	//ANLEITUNG:
    /* 
    * Hier werden die Kalibrierungsdaten geladen. Die Kalibrierungsdaten sind in einer JSON-Datei gespeichert und stammen von der 2D-Kamera.
    * Die 2D-Kamera liefert einen Rotationsvektor und einen Translationsvektor. Diese Vektoren werden in eine Rotationsmatrix umgewandelt.
    * Durch diese Vektoren kann man die Koordinatensysteme der zwei Kameras miteinander verbinden.
    * Die Rotationsmatrix wird durch die Rodrigues-Formel berechnet.
    */
    std::ifstream file(pFileName);

    if (!file)
    {
        throw(1);
    }

    json jsonData;
    file >> jsonData;

    auto rvecArray = jsonData["Rvecs"][0];
    Eigen::Vector3f rvec(rvecArray[0][0], rvecArray[1][0], rvecArray[2][0]);
    auto tvecArray = jsonData["Tvecs"][0];
    Eigen::Vector3f tvec(tvecArray[0][0], tvecArray[1][0], tvecArray[2][0]);

    tvec.x() = tvec.x() + this->aParameters.xKoordinateOffset;
    tvec.y() = tvec.y() + this->aParameters.yKoordinateOffset;
    tvec.z() = this->aParameters.zKoordinateOffset;

    Eigen::Matrix3f rotationMatrix = RodriguesToRotationMatrix(rvec);
    Eigen::Matrix4f transformMatrix = Eigen::Matrix4f::Identity();

    transformMatrix.block<3, 3>(0, 0) = rotationMatrix;
    transformMatrix.block<3, 1>(0, 3) = tvec;
    this->aCalibrationDataLoaded = true;

    return transformMatrix;
}

void Prozessing::ApplyROI(pcl::PointCloud<pcl::PointXYZ>::Ptr inputCloud, float xMin, float xMax, float yMin, float yMax)
{
	//ANLEITUNG:
    /* 
    * Die Funktion ApplyROI wird verwendet um die Punktwolke auf einen bestimmten Bereich zu beschränken.
    * Das wird direkt am Anfang gemacht um die Punktwolke zu verkleinern und somit die Rechenzeit zu verringern.
    * Man könnte nach der Kalibrierung nochmals die Punktwolke auf einen bestimmten Bereich beschränken, jedoch wird das Später durch die
    * Funktion RemoveGroundByZHistogram gemacht. Damit wird der Hintergrund entfernt und nur die Objekte in der Punktwolke bleiben.
    *
    */
    pcl::CropBox<pcl::PointXYZ> cropBoxFilter;
    cropBoxFilter.setInputCloud(inputCloud);

    Eigen::Vector4f minPoint(xMin, yMin, -std::numeric_limits<float>::max(), 1.0);
    Eigen::Vector4f maxPoint(xMax, yMax, std::numeric_limits<float>::max(), 1.0);

    cropBoxFilter.setMin(minPoint);
    cropBoxFilter.setMax(maxPoint);
   

    cropBoxFilter.filter(*inputCloud);
}


//--------------------ProcessPoints-----------------------//
void Prozessing::FitParabolicCylinder(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud, pcl::ModelCoefficients::Ptr& cylinder_coeffs, pcl::PointIndices::Ptr& cylinder_inliers)
{
	//ANLEITUNG:
    /*
    * WICHTIG:
	* Diese Funktion wird nicht mehr verwendet. Man kann mit dieser Funktion eine Art Objekterkennng machen. 
	* Durch die Anzahl an Inliers kann man dann bestimmen um welches Objekt es sich handelt -> Der Algorithmus mit den meisten Inliers wird dann weiter verarbeitet.
    * 
	* Es ist schwierig die passenden Parameter zu finden, da die Inliers oft sehr nah beieinander liegen.
	* Gelöst wurde es durch eine 2D-Objekterkennung welche der 3D-Anwendung direkt sagt um welches Objekt es sich handelt.
    * 
    * 
    Die Funktion von segmentation Algorithmus funkitoniert hier nicht da er kein Zylinder erkennt sondern nur einen Parabolischen Zylinder.
    segmentationAlgorithm.setOptimizeCoefficients(true);
    segmentationAlgorithm.setInputCloud(local_cloud);
    segmentationAlgorithm.setModelType(pcl::SACMODEL_ZYLINDER);
    segmentationAlgorithm.setMethodType(pcl::SAC_RANSAC);

    Um Trotzdem einen Zylinder muss diese Funktion verwendet werden. Hierzu wurd allgemein die Funktion für ein Zylinder definiert  z = a x^2 + b y^2 + c x + d y + e
    Dann werden 5 Punkte zufällig ausgewählt und die Koeffizienten a,b,c,d,e berechnet.
    Dann wird geprüft ob die Punkte zum Modell passen und ob es genug Inliers gibt.
    Das wird solange wiederholt bis das beste Modell gefunden wurde.

    Nachdem das beste Modell gefunden wurde, werden die Koeffizienten und die Inliers gespeichert.
    Die Inliers werden dann mit anderen Modellen verglichen ob es eventuell nicht doch um eine Plane handelt
    Der wo mehr Inliers hat wird dann weiter verarbeitet
    */
    const int max_iterations = this->aParameters.RANSACmaxIteration;
    const float distance_threshold = this->aParameters.cylinderDetectionThreshold;
    const int min_inliers = 100;
   
    std::vector<int> best_inliers;
    Eigen::VectorXd best_coefficients(5);
    //Gleichung die es zu lösen gilt: z = a x^2 + b y^2 + c x + d y + e

    for (int iteration = 0; iteration < max_iterations; ++iteration)
    {
        // 1. Wähle zufällig 5 Punkte P={(x1,y1,z1), ..., (x5, y5, z5)}
        std::vector<int> sample_indices;
        while (sample_indices.size() < 5) {
            int index = rand() % cloud->points.size();
            if (std::find(sample_indices.begin(), sample_indices.end(), index) == sample_indices.end())
            {
                sample_indices.push_back(index);
            }
        }

        // Erstelle Design-Matrix A und Ziel-Vektor b
        Eigen::MatrixXd A(5, 5);
        Eigen::VectorXd b(5);

        for (int i = 0; i < 5; ++i)
        {
            float x = cloud->points[sample_indices[i]].x;
            float y = cloud->points[sample_indices[i]].y;
            float z = cloud->points[sample_indices[i]].z;

            A(i, 0) = x * x;
            A(i, 1) = y * y;
            A(i, 2) = x;
            A(i, 3) = y;
            A(i, 4) = 1.0;
            b(i) = z;
        }

        // 2. Löse für die Koeffizienten https://cvgl.stanford.edu/teaching/cs231a_winter1415/lecture/lecture9_fitting_matching.pdf S.16

        Eigen::VectorXd coefficients = A.jacobiSvd(Eigen::ComputeThinU | Eigen::ComputeThinV).solve(b);


        // 3. Zähle Inliers basierend auf der Distanz zum Modell
        std::vector<int> inliers;
        for (size_t i = 0; i < cloud->points.size(); ++i)
        {
            float x = cloud->points[i].x;
            float y = cloud->points[i].y;
            float z = cloud->points[i].z;
            //      z    =              a x^2                     + b y^2              + c x                   + d y                    + e
            float z_pred = (coefficients(0) * x * x) + (coefficients(1) * y * y) + (coefficients(2) * x) + (coefficients(3) * y) + (coefficients(4));

            if (std::abs(z - z_pred) <= distance_threshold)
            {
                //Prüft ob der Punkt ein Inlier ist (also ob der Punkt zum Modell passt)
                inliers.push_back(i);
            }
        }

        // 4. Speichere das beste Modell basierend auf der Anzahl der Inliers
        if (inliers.size() > best_inliers.size() && inliers.size() >= min_inliers)
        {
            best_inliers = inliers;
            best_coefficients = coefficients;
        }
    }

    // 5. Prüfe, ob ein sinnvolles Modell gefunden wurde
    if (best_inliers.empty())
    {
        return;
    }

    // 6. Speichere die besten Koeffizienten und Inliers
    cylinder_inliers->indices = best_inliers;
    cylinder_coeffs->values.resize(5);
    for (size_t i = 0; i < 5; ++i)
    {
        cylinder_coeffs->values[i] = best_coefficients(i);
    }
    if ((std::abs(cylinder_coeffs->values[0]) < 0.0002) || (std::abs(cylinder_coeffs->values[1]) < 0.0002))
    {
		// Wenn die Koeffizienten für a oder b sehr klein sind, ist es wahrscheinlich keine Parabel, es handelt sich dann um ein Rechteck (Ebene)
        cylinder_inliers->indices.clear();
        cylinder_coeffs->values.clear();
        return;
    }

}

bool IsInRotatedRectangle(float px, float py, float cx, float cy, float width, float height, float angle_rad)
{
    float dx = px - cx;
    float dy = py - cy;

    // Drehe Punkt zurück (inverse Rotation)
    float rotatedX = dx * std::cos(-angle_rad) - dy * std::sin(-angle_rad);
    float rotatedY = dx * std::sin(-angle_rad) + dy * std::cos(-angle_rad);

    return std::abs(rotatedX) <= width / 2.0f && std::abs(rotatedY) <= height / 2.0f;
}


ObjectPose Prozessing::EstimateObjectPose(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud, float x_center, float y_center, float radius,int objektType,float angle_objekt)
{
	//ANLEITUNG:
	/*
	* In dieser Funktion wird die Pose eines Objekts geschätzt, basierend auf den gegebenen Parametern.
    * 
    */ 


    this->objectCount++;
    // Hintergrund-Ebenenkoeffizienten
    float A = this->aUndergroundCoeffs->values[0];
    float B = this->aUndergroundCoeffs->values[1];
    float C = this->aUndergroundCoeffs->values[2];
    float D = this->aUndergroundCoeffs->values[3];
    ObjectPose pose;
    Eigen::Vector3f centroid(0.0, 0.0, 0.0);
    Eigen::Affine3f transform = Eigen::Affine3f::Identity();
   
   
	//----Rechteck----//
    if (objektType == 0)  
    {
        pcl::PointCloud<pcl::PointXYZRGB>::Ptr local_cloud(new pcl::PointCloud<pcl::PointXYZRGB>);
        float width = 7 * radius;    // oder ein fester Wert
        float height = radius;       // z.B. halb so hoch
        float angle_rad = angle_objekt * M_PI / 180.0f;

		// Filtere die Punkte, die innerhalb des Rechtecks liegen
		// Hier wird ein Rechteck um den Mittelpunkt (x_center, y_center) mit der Breite und Höhe erstellt

        for (const auto& point : cloud->points)
        {
            if (IsInRotatedRectangle(point.x, point.y, x_center, y_center, width, height, angle_rad))
            {
				// Wenn der Punkt innerhalb des Rechtecks liegt, füge ihn zum lokalen Cloud hinzu
                local_cloud->points.push_back(point);
            }
        }
		if (local_cloud->points.empty())
		{
			return pose; // Keine Punkte im Rechteck gefunden
		}

		// Filtere Rauschen aus der lokalen Punktwolke
        pcl::StatisticalOutlierRemoval<pcl::PointXYZRGB> sor;
        sor.setInputCloud(local_cloud);
        sor.setMeanK(this->aParameters.statisticalOutlierRemovalNeighbors);
        sor.setStddevMulThresh(this->aParameters.statisticalOutlierRemovalRadius);
        sor.filter(*local_cloud);

		// Radius Outlier Removal anwenden, um weitere Ausreißer zu entfernen
        pcl::RadiusOutlierRemoval<pcl::PointXYZRGB> ror;
        ror.setInputCloud(local_cloud);
        ror.setRadiusSearch(this->aParameters.RadiusOutlierRemovalRadius);
        ror.setMinNeighborsInRadius(this->aParameters.RadiusOutlierRemovalMinNeighbors);
        ror.filter(*local_cloud);


        Eigen::MatrixXf points(3, local_cloud->points.size());

        // Die übergebenen Werte direkt verwenden
        Eigen::Vector3f centroid(x_center, y_center, 0.0f);
        float min_z = std::numeric_limits<float>::max();
        float max_z = std::numeric_limits<float>::lowest();
        Eigen::Vector3f min_z_point, max_z_point;

        for (size_t i = 0; i < local_cloud->points.size(); ++i)
        {
            Eigen::Vector3f p(local_cloud->points[i].x, local_cloud->points[i].y, local_cloud->points[i].z);
            points.col(i) = p;

            // Min/Max Z-Werte speichern
            min_z = std::min(min_z, p.z());
            max_z = std::max(max_z, p.z());
        }

        // Setze den Z-Wert des Schwerpunkts
        centroid.z() = (min_z + max_z) / 2.0f;

        // Kovarianzmatrix berechnen
        Eigen::MatrixXf centered_points = points.colwise() - centroid;
        Eigen::Matrix3f covariance = (centered_points * centered_points.transpose()) / local_cloud->points.size();

        // Eigenwertzerlegung (PCA)
        Eigen::SelfAdjointEigenSolver<Eigen::Matrix3f> eigen_solver(covariance);
        Eigen::Matrix3f eigen_vectors = eigen_solver.eigenvectors();

        // Achsen bestimmen
        Eigen::Vector3f length_axis = eigen_vectors.col(2);  // Längste Achse (X)
        Eigen::Vector3f width_axis = eigen_vectors.col(1);   // Zweitlängste Achse (Y)
        Eigen::Vector3f height_axis = eigen_vectors.col(0);  // Kleinste Achse (Z)

        // Sicherstellen, dass die längste Achse wirklich entlang der langen Seite zeigt
        float length_1 = (points.colwise() - centroid).rowwise().norm().maxCoeff();
        float length_2 = (points.colwise() - centroid).rowwise().norm().maxCoeff();

        if (length_1 < length_2)
        {
            std::swap(length_axis, width_axis);
        }

       
       // Höhenachse anpassen (soll nach unten zeigen, aber mit leichter Neigung erlaubt)
        Eigen::Vector3f up_vector(A, B, C);
        up_vector.normalize();

        // Prüfe, ob die PCA-Z-Achse stark von der Untergrundnormalen abweicht
        if (height_axis.dot(up_vector) < 0) 
        {
            height_axis = -height_axis;  // Richtung korrigieren
        }

        // Begrenze die Neigung der Z-Achse zur Untergrundnormalen (max. 45° Pitch erlaubt)
        float max_angle = M_PI / 4; // 45° in Radiant
        float angle = std::acos(height_axis.dot(up_vector));

        if (angle > max_angle) 
        {
            Eigen::Vector3f adjusted_z = (height_axis + up_vector).normalized();
            height_axis = adjusted_z;
        }

        // Berechne eine stabile X-Achse, die mit der längsten Achse ausgerichtet ist
        Eigen::Vector3f x_axis = length_axis - length_axis.dot(height_axis) * height_axis;
        x_axis.normalize();

        // Berechne Y-Achse aus Kreuzprodukt von Z- und X-Achse
        Eigen::Vector3f y_axis = height_axis.cross(x_axis).normalized();

        // Rotationsmatrix erstellen
        Eigen::Matrix3f rotation_matrix;
        rotation_matrix.col(0) = x_axis;
        rotation_matrix.col(1) = y_axis;
        rotation_matrix.col(2) = height_axis;

        // Berechnung der Quader-Dimensionen
        Eigen::Vector3f min_point(FLT_MAX, FLT_MAX, FLT_MAX);
        Eigen::Vector3f max_point(-FLT_MAX, -FLT_MAX, -FLT_MAX);

        // Transformiere alle Punkte ins lokale Koordinatensystem
        Eigen::Affine3f transform = Eigen::Affine3f::Identity();
        transform.linear() = rotation_matrix;
        transform.translation() = centroid;

        for (const auto& p : local_cloud->points) {
            Eigen::Vector3f pt = transform.inverse() * Eigen::Vector3f(p.x, p.y, p.z);
            min_point = min_point.cwiseMin(pt);
            max_point = max_point.cwiseMax(pt);
        }

        // Berechne Abmessungen
        Eigen::Vector3f dimensions = max_point - min_point;

        // Quader-Parameter
        Eigen::Vector3f box_center = centroid;
        Eigen::Quaternionf box_quat(rotation_matrix);

        this->aPCLVisualizer->addCube
        (
            centroid, box_quat,
            dimensions.x(), dimensions.y(), dimensions.z(),
            std::to_string(this->objectCount)
        );


        // Style-Einstellungen
        this->aPCLVisualizer->setShapeRenderingProperties
        (
            pcl::visualization::PCL_VISUALIZER_COLOR,
            0.0, 1.0, 0.0,  // RGB (grün)
            std::to_string(this->objectCount)
        );

        this->aPCLVisualizer->setShapeRenderingProperties
        (
            pcl::visualization::PCL_VISUALIZER_OPACITY,
            0.5,
            std::to_string(this->objectCount)
        );

        // Pose setzen
        pose.position = centroid;
        pose.position.z() = this->aZPlane - pose.position.z();
        pose.orientation = rotation_matrix;
        pose.type = 0;
       

        // Koordinatensystem visualisieren
        transform.translation() = centroid;
        transform.linear() = pose.orientation;
        this->aPCLVisualizer->addCoordinateSystem
        (
            this->aParameters.coorSystemScale,
            transform,
            std::to_string(this->objectCount),
            0
        );
        return pose;
    }

   else if (objektType == 1) // Zylinder
   {
       //----- Zylinder -----//
       pcl::PointCloud<pcl::PointXYZRGB>::Ptr local_cloud(new pcl::PointCloud<pcl::PointXYZRGB>);
       float width = radius;   
       float height = radius/8;     
       float angle_rad = angle_objekt * M_PI / 180.0f;

	   // Filtere die Punkte, die innerhalb des Zylinders liegen
       for (const auto& point : cloud->points)
       {
           if (IsInRotatedRectangle(point.x, point.y, x_center, y_center, width, height, angle_rad))
           {
			   // Wenn der Punkt innerhalb des Zylinders liegt, füge ihn zum lokalen Cloud hinzu
               local_cloud->points.push_back(point);
           }
       }
	   if (local_cloud->points.size() == 0)
	   {
		   return pose;
	   }
	   // Filtere Rauschen aus der lokalen Punktwolke
       pcl::StatisticalOutlierRemoval<pcl::PointXYZRGB> sor;
       sor.setInputCloud(local_cloud);
       sor.setMeanK(this->aParameters.statisticalOutlierRemovalNeighbors);
       sor.setStddevMulThresh(this->aParameters.statisticalOutlierRemovalRadius);
       sor.filter(*local_cloud);

      

       
       Eigen::MatrixXf points(3, local_cloud->points.size());

       for (size_t i = 0; i < local_cloud->points.size(); ++i)
       {
           points.col(i) = Eigen::Vector3f(local_cloud->points[i].x, local_cloud->points[i].y, local_cloud->points[i].z);
       }

       // Berechne den Schwerpunkt der Punktwolke
       centroid = points.rowwise().mean();

       // Subtrahiere den Schwerpunkt, um die Punktwolke zu zentrieren
       Eigen::MatrixXf centered_points = points.colwise() - centroid;

       // Berechne die Kovarianzmatrix
       Eigen::Matrix3f covariance = (centered_points * centered_points.transpose()) / local_cloud->points.size();

       // Eigenwertzerlegung durchführen
       Eigen::SelfAdjointEigenSolver<Eigen::Matrix3f> eigen_solver(covariance);
       Eigen::Vector3f cylinder_axis = eigen_solver.eigenvectors().col(2); // Hauptachse
       cylinder_axis.normalize();

       // Berechne die Ausdehnung entlang der Zylinderachse
       float min_proj = std::numeric_limits<float>::max();
       float max_proj = std::numeric_limits<float>::lowest();

       for (const auto& point : local_cloud->points) 
       {
           Eigen::Vector3f pt(point.x, point.y, point.z);
           float projection = pt.dot(cylinder_axis);
           min_proj = std::min(min_proj, projection);
           max_proj = std::max(max_proj, projection);
       }

       // Künstliche Verlängerung des Zylinders (Faktor 1.5 für 50% längeren Zylinder)
       float length_extension_factor = 1.0f;
       float original_length = max_proj - min_proj;
       float extended_length = original_length * length_extension_factor;
       float length_diff = (extended_length - original_length) / 2.0f;

       min_proj -= length_diff;
       max_proj += length_diff;

       // Mittelpunkt des verlängerten Zylinders
       Eigen::Vector3f extended_centroid = centroid;

       // Zylinderparameter für Visualisierung
       pcl::ModelCoefficients cylinder_coeff;
       cylinder_coeff.values.resize(7);

       // Startpunkt des Zylinders (zentriert)
       cylinder_coeff.values[0] = centroid.x() - (cylinder_axis.x() * extended_length / 2);
       cylinder_coeff.values[1] = centroid.y() - (cylinder_axis.y() * extended_length / 2);
       cylinder_coeff.values[2] = centroid.z() - (cylinder_axis.z() * extended_length / 2);

       // Richtungsvektor (zeigt entlang der verlängerten Achse)
       cylinder_coeff.values[3] = cylinder_axis.x() * extended_length;
       cylinder_coeff.values[4] = cylinder_axis.y() * extended_length;
       cylinder_coeff.values[5] = cylinder_axis.z() * extended_length;

       // Radius des Zylinders (kann angepasst werden)
       float radius = 13.0f; // Ihr gewünschter Radius
       cylinder_coeff.values[6] = radius;

       
       this->aPCLVisualizer->addCylinder(cylinder_coeff, std::to_string(this->objectCount), 0);

       // Visuelle Eigenschaften
       this->aPCLVisualizer->setShapeRenderingProperties
       (
           pcl::visualization::PCL_VISUALIZER_COLOR,
           1.0, 0.0, 0.0,  // Rot
           std::to_string(this->objectCount)
       );

       this->aPCLVisualizer->setShapeRenderingProperties
       (
           pcl::visualization::PCL_VISUALIZER_OPACITY,
           0.7,
           std::to_string(this->objectCount)
       );

       // Koordinatensystem
       Eigen::Affine3f transform = Eigen::Affine3f::Identity();
       transform.translation() = extended_centroid;
       transform.linear().col(0) = cylinder_axis;                  // X-Achse = Zylinderachse
       transform.linear().col(1) = cylinder_axis.unitOrthogonal(); // Y-Achse orthogonal
       transform.linear().col(2) = cylinder_axis.cross(transform.linear().col(1)); // Z-Achse

       this->aPCLVisualizer->addCoordinateSystem
       (
           this->aParameters.coorSystemScale,
           transform,
          std::to_string(this->objectCount++),
           0
       );

       pose.position = extended_centroid;
       pose.orientation = transform.linear();
	   pose.position.z() = this->aZPlane - pose.position.z();

       pose.type = 1;

       return pose;
   }

   else if (objektType == 2) // Kreis (Zylinder)
    {
        pcl::PointCloud<pcl::PointXYZRGB>::Ptr local_cloud(new pcl::PointCloud<pcl::PointXYZRGB>);
        
		// Filtere die Punkte, die innerhalb des Kreises liegen
        for (const auto& point : cloud->points)
        {
            float dx = point.x - x_center;
            float dy = point.y - y_center;
            if (std::sqrt(dx * dx + dy * dy) <= radius)
            {
                local_cloud->points.push_back(point);
            }
        }

        std::vector<float> z_values;
        for (const auto& point : local_cloud->points)
        {
            z_values.push_back(point.z);
        }

        if (z_values.empty())
        {
            return ObjectPose();
        }

        // 1. Min/Max Z-Werte bestimmen (Zylinderhöhe)
        float min_z = *std::min_element(z_values.begin(), z_values.end());
        float max_z = *std::max_element(z_values.begin(), z_values.end());
        float height = max_z - min_z;

        if (height <= 0)
        {
            return ObjectPose();
        }

        // 2. Pose auf höchste Ebene setzen (min_z ist vom Betrag größer)
        Eigen::Vector3f topCenter(x_center, y_center, min_z);
        pose.position = topCenter;
        pose.orientation = Eigen::Matrix3f::Identity(); // Zylinder steht senkrecht
        pose.type = 2;

        // 3. Zylinderparameter für PCL-Visualisierung
        pcl::ModelCoefficients cylinder_coeff;
        cylinder_coeff.values.resize(7);
        cylinder_coeff.values[0] = x_center;
        cylinder_coeff.values[1] = y_center;
        cylinder_coeff.values[2] = min_z; // Basis

        cylinder_coeff.values[3] = 0.0;
        cylinder_coeff.values[4] = 0.0;
        cylinder_coeff.values[5] = height; // Richtung entlang Z (von Basis zu top)

        cylinder_coeff.values[6] = radius;

        this->aPCLVisualizer->addCylinder
        (
            cylinder_coeff,
            std::to_string(this->objectCount),
            0
        );

        this->aPCLVisualizer->setShapeRenderingProperties
        (
            pcl::visualization::PCL_VISUALIZER_COLOR,
            0.0, 0.0, 1.0,
            std::to_string(this->objectCount)
        );

        this->aPCLVisualizer->setShapeRenderingProperties
        (
            pcl::visualization::PCL_VISUALIZER_OPACITY,
            0.5,
            std::to_string(this->objectCount)
        );

        // 4. Koordinatensystem am höchsten Punkt 
        Eigen::Affine3f transform = Eigen::Affine3f::Identity();
        transform.translation() = pose.position;
        transform.linear() = pose.orientation;

        this->aPCLVisualizer->addCoordinateSystem(
            this->aParameters.coorSystemScale, transform,
            std::to_string(this->objectCount),
            0
        );

        return pose;
   }
}

pcl::PointCloud<pcl::PointXYZ>::Ptr Prozessing::ScalePointCloud(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud, float scaleFactor)
{
	//ANLEITUNG:
	/*
	* Bei der SICK-Kamera ist die Punktwolke sehr dich beieinander. Und somit sehr klein.
	* Um die Punktwolke besser visualisieren zu können, wird die Punktwolke skaliert.
    */
    pcl::PointCloud<pcl::PointXYZ>::Ptr scaledCloud(new pcl::PointCloud<pcl::PointXYZ>);
    for (const auto& pt : cloud->points)
    {
        pcl::PointXYZ scaledPoint;
        scaledPoint.x = pt.x * scaleFactor * this->aParameters.xKoordianteSkaling;
        scaledPoint.y = pt.y * scaleFactor * this->aParameters.yKoordianteSkaling;
        scaledPoint.z = pt.z * scaleFactor * this->aParameters.zKoordianteSkaling;
        scaledCloud->points.push_back(scaledPoint);
    }
    scaledCloud->width = scaledCloud->points.size();
    scaledCloud->height = 1;
    scaledCloud->is_dense = cloud->is_dense;

    return scaledCloud;
}
pcl::PointCloud<pcl::PointXYZ>::Ptr Prozessing::TransformCoordinateSystem(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{
	//ANLEITUNG:
	/*
	* Wird nur bei der Sick Kamera verwendet.
	* Leider hat die Formel mit der Rotationsmatrix nur bei Schmersal funktioniert->Grund ist noch Unklar
    * Deshalb muss die Punktwolke "Von Hand" verschoben und Rotiert bzw. gespiegelt werden
    */
    pcl::PointCloud<pcl::PointXYZ>::Ptr transformedCloud(new pcl::PointCloud<pcl::PointXYZ>);

    Eigen::Affine3f transform = Eigen::Affine3f::Identity();

    // Matrix: Spiegelung in X und Y
    Eigen::Matrix3f mirror;
    mirror << -1, 0, 0,
        0, -1, 0,
        0, 0, 1;

    transform.linear() = mirror;

    // Translation hinzufügen
    transform.translation() = Eigen::Vector3f(this->aParameters.xKoordinateOffset, this->aParameters.yKoordinateOffset, this->aParameters.zKoordinateOffset);

   

    pcl::transformPointCloud(*cloud, *transformedCloud, transform);

    transformedCloud->width = transformedCloud->points.size();
    transformedCloud->height = 1;
    transformedCloud->is_dense = cloud->is_dense;

    return transformedCloud;
}

//--------------------Main-Function-----------------------//
int Prozessing::ProcessPoints(Point2D* inputPoints, int inputCount, ObjectPose* outputPoses, int* outputCount)
{
    /*ANLEITUNG:
    * Das ist die Hauptfunktion (Main) welcher alle Funktionen zusammenführt.
    * Als Input komme ein Array voller Mittelpunkte und Radien der Objekte.
    * Dieser Input kommt von der Kamera. Der Radius wird genutzt um eine Lokale Punktwolke um das Objekt zu erstellen.
    *
    * Als erstes wird die Kalibrierungsdaten geladen um die Punktwolke auf 2D-Ebene zu kalibrieren.
    * Danach wird die Punktwolke geladen und auf einen bestimmten Bereich beschränkt.
    * Dann wird die Punktwolke 2D-Ebene durch die Kalibrierungsdaten kalibriert (transforPointCloud).
    * Dann wird es auf eine Z-Ebene gebracht (CalibratePointcloud) und der Hintergrund entfernt (RemoveGroundByZHistogram).
    *
    * Ist das alles erleigt wird die Punktwolke eingefärbt (ColorizePointCloud) und die Visualisierung aktualisiert (UpdatePointCloud).
    * Gleichzeitig wird die Position und Orientierung der Objekte geschätzt (EstimateObjectPose).
    *
    * Ist das alles fertig wird die Orientierung im Raum und die Position der Objekte zurückgegeben.
   */
    try
    {
        if (inputPoints == nullptr || outputPoses == nullptr || outputCount == nullptr)
        {
            return 4;
        }

        pcl::PointCloud<pcl::PointXYZ>::Ptr transformedCloud(new pcl::PointCloud<pcl::PointXYZ>());
        pcl::PointCloud<pcl::PointXYZ>::Ptr modifiedCloud(new pcl::PointCloud<pcl::PointXYZ>());


        if (!this->aCalibrationDataLoaded || this->aTransformMatrix.isIdentity())
        {
            this->aTransformMatrix = LoadCalibrationData(this->aParameters.calibrationPath);
        }

        if (this->aParameters.selectedCamera == 2)
        {
            if (pcl::io::loadPCDFile(this->aParameters.dataPath, *this->aOrginalCloud) == -1)
            {
                return 5;
			}
		}
        else if (this->aParameters.selectedCamera == 1)
        {
            if (pcl::io::loadPLYFile(this->aParameters.dataPath, *this->aOrginalCloud) == -1)
            {
                return 5;
            }
        }

        if (this->aParameters.selectedCamera == 1) //Sick
        {
            *transformedCloud = *this->aOrginalCloud;
            modifiedCloud = ScalePointCloud(transformedCloud, this->aParameters.pointCloudScale);
            modifiedCloud = RemoveGroundByZHistogram(CalibratePointCloud(modifiedCloud));
            modifiedCloud = TransformCoordinateSystem(modifiedCloud);
			ApplyROI(modifiedCloud, this->aROILowerLimit.x(), this->aROIUpperLimit.x(), this->aROILowerLimit.y(), this->aROIUpperLimit.y());
          
        }
        else if (this->aParameters.selectedCamera == 2) //Schmersal
        {
            ApplyROI(this->aOrginalCloud, this->aROILowerLimit.x(), this->aROIUpperLimit.x(), this->aROILowerLimit.y(), this->aROIUpperLimit.y());
            transformPointCloud(*this->aOrginalCloud, *transformedCloud, this->aTransformMatrix);
            modifiedCloud = RemoveGroundByZHistogram(CalibratePointCloud(transformedCloud));
        
        }
       

        this->aColorCloud = ColorizePointCloud(modifiedCloud);
        UpdatePointCloud(this->aColorCloud);

        //Wollen hier Objekte gelöscht werden, welche in dem Visulizer platziert werden müssen sie mit dieser Logik gelöscht werden
        //Problem ist jedoch das es aus irgendeinem Grund nur einmal Funktioniert, beim zweiten Löschen kann es sein das das Programm abstürzt weil er 
        //den Pointer oder die Adresse verliert
		//Die Vermutung ist das es an den geteilten Resourcen liegt, da die Visualisierung und die Verarbeitung der Punkte in einem Thread laufen.
		//GGF. Kann man versuchen mit std::mutex zu arbeiten, um die Resourcen zu schützen. Dies wurde aufgrund von Zeitmangel noch nicht ausprobiert.
      
		//if (this->objectCount > 0)
		//{
		//	for (this->objectCount; this->objectCount > 0; this->objectCount--)
		//	{
		//		this->aPCLVisualizer->removeShape(std::to_string(this->objectCount));
        //              //RemoveShape verursacht einen Fehler in der Engine beim ersten mal löschen,
        //              //beim zweiten mal kann es sein das es entwerder geht oder abstürzt -> Grund ist unklar.
		//		this->aPCLVisualizer->removeCoordinateSystem(std::to_string(this->objectCount));
		//	}
		//	this->objectCount = 0;
		//}

       
        int poseCount = 0;
        for (int i = 0; i < inputCount; i++)
        {
			ObjectPose pose = EstimateObjectPose(this->aColorCloud, inputPoints[i].x, inputPoints[i].y, inputPoints[i].radius, inputPoints[i].objektType, inputPoints[i].winkel);
            outputPoses[poseCount] = pose;
            poseCount++;

        }

        // Aktualisieren Sie den tatsächlichen Output-Count
        *outputCount = poseCount;

        // Speicher freigeben
        modifiedCloud.reset();
        transformedCloud.reset();

        return 0;
    }
    catch (int pException)
    {
        return pException;
    }

}

