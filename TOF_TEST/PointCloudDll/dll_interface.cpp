#include "cloud_viewer.h"
#include "cloud_processing.h"
#include "SickKamera.h"
#define EXPORT_METHOD extern "C" __declspec(dllexport)
Prozessing processing;


//Das ist ist das Interface
//Hier werden die Methoden definiert, die von C# aufgerufen werden können


EXPORT_METHOD void StartVisualizerThread(Parameters pParameter)
{
	
	processing.StartVisualizerThread(pParameter);
}

EXPORT_METHOD void StopVisualizerThread()
{
    processing.StopVisualizerThread();
}
EXPORT_METHOD int ProcessPoints(Point2D* inputPoints, int inputCount, ObjectPose* outputPoses, int* outputCount)
{
	return processing.ProcessPoints(inputPoints, inputCount, outputPoses, outputCount);
}


