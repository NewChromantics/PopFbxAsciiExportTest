using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;



public class TransformTracker
{

	Transform targetTransform;

	bool recordPos = false;
	bool recordRot = false;
	bool recordScale = false;

	public List<Vector3> posDataList;
	public List<Quaternion> rotDataList;
	public List<Vector3> scaleDataList;

	public TransformTracker(Transform targetObj, bool trackPos, bool trackRot, bool trackScale)
	{

		targetTransform = targetObj;

		if (trackPos)
		{
			posDataList = new List<Vector3>();
			recordPos = trackPos;
		}

		if (trackRot)
		{
			rotDataList = new List<Quaternion>();
			recordRot = trackRot;
		}

		if (trackScale)
		{
			scaleDataList = new List<Vector3>();
			recordScale = trackScale;
		}
	}

	public void recordFrame()
	{
		if (recordPos)
			posDataList.Add(targetTransform.localPosition);

		if (recordRot)
			rotDataList.Add(targetTransform.localRotation);

		if (recordScale)
			scaleDataList.Add(targetTransform.localScale);
	}
}



public class FbxExporter : MonoBehaviour
{

	public string sourceFilePath;
	public string exportFileFolder;
	public string exportFilePath;

	FbxObjectsManager fbxObj;
	FbxConnectionsManager fbxConn;

	// objs to track
	Transform[] observeTargets;
	//TransformTracker[] trackers;
	int objNums = -1;
	//objNums = trackers.Length;


	// record operation settings
	public KeyCode startRecordKey = KeyCode.Q;
	public KeyCode endRecordKey = KeyCode.W;

	// export settings
	bool includePathName = false;
	bool recordPos = true;
	bool recordRot = true;
	bool recordScale = true;




	void ModifyDefinitions (string targetFilePath)
	{

		Debug.Log ("Generate Correct Definition Node ..");

		FbxDataNode[] nodes = FbxDataNode.FetchNodes (File.ReadAllText (targetFilePath), 0);
		int defIndex = 0;

		for (int i = 0; i < nodes.Length; i++) {
			if (nodes [i].nodeName == "Definitions") {
				defIndex = i;
				break;
			}
		}

		FbxDataNode AnimationCurveNode = new FbxDataNode ("ObjectType", "\"AnimationCurveNode\"", 1);
		AnimationCurveNode.addSubNode (new FbxDataNode ("Count", (observeTargets.Length * 3).ToString (), 2));

		FbxDataNode ObjectTemplateNode = new FbxDataNode ("PropertyTemplate", "\"FbxAnimCurveNode\"", 2);
		FbxDataNode propertiesNode = new FbxDataNode ("Properties70", " ", 3);
		propertiesNode.addSubNode (new FbxDataNode ("P", "\"d\", \"Compound\", \"\", \"\"", 4));

		ObjectTemplateNode.addSubNode (propertiesNode);
		AnimationCurveNode.addSubNode (ObjectTemplateNode);


		FbxDataNode AnimationCurve = new FbxDataNode ("ObjectType", "\"AnimationCurve\"", 1);
		AnimationCurve.addSubNode (new FbxDataNode ("Count", "10", 2));

		nodes [defIndex].addSubNode (AnimationCurveNode);
		nodes [defIndex].addSubNode (AnimationCurve);

		//Debug.Log (nodes [defIndex].getResultData ());
		Debug.Log ("Replacing Definition Node ..");

		// find line
		StreamReader reader = new StreamReader (targetFilePath);
		string headContent = "";
		string footContent = "";

		while (reader.Peek () != -1) {
			string line = reader.ReadLine ();

			if (line.IndexOf ("Definitions") != -1) {
				break;
			} else
				headContent += line + "\n";
		}

		int bracketNum = 1;

		while (reader.Peek () != -1) {
			string line = reader.ReadLine ();

			if (line.IndexOf ("{") != -1) {
				++bracketNum;
			} else if (line.IndexOf ("}") != -1) {
				--bracketNum;

				if (bracketNum == 0)
					break;
			}
		}

		footContent = reader.ReadToEnd ();
		reader.Close ();
		string defResultData = nodes [defIndex].getResultData ();

		Debug.Log (headContent);
		Debug.Log (footContent);
		File.WriteAllText (targetFilePath, headContent + defResultData + footContent);
	}
	/*
	void ExportToFile (System.Action<string> WriteLine,List<AnimObject> AnimObjects)
	{

		Debug.Log ("copy file ...");

		// copy Data into New Data, and clear preRotations
		StreamWriter writer = new StreamWriter(exportFilePath);
		StreamReader reader = new StreamReader (sourceFilePath);

		while (reader.Peek () != -1) {
			string strLine = reader.ReadLine ();

			// find prerotation
			if (strLine.IndexOf ("PreRotation") != -1) {

				// find tabs before 
				int tabNum = strLine.IndexOf ("P:") + 1;
				string tabStr = "";

				for (int i = 0; i < tabNum; i++)
					tabStr += "\t";

				// just simply replace whole line, since every attribute looks the same
				writer.WriteLine (tabStr + "P: \"PreRotation\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			} else
				writer.WriteLine (strLine);
		}
		writer.Close ();
		reader.Close ();





		Debug.Log ("fetch nodes ...");


		FbxDataNode[] allNodes = FbxDataNode.FetchNodes (File.ReadAllText (exportFilePath), 0);
		int objNodeIndex = 0;

		for (int i = 0; i < allNodes.Length; i++) {
			if (allNodes [i].nodeName == "Objects")
				objNodeIndex = i;
		}



		// setup converter
		fbxObj = new FbxObjectsManager (allNodes[objNodeIndex], exportFileFolder);
		fbxConn = new FbxConnectionsManager (File.ReadAllText (exportFilePath));

		string animBaseLayerId = fbxConn.getAnimBaseLayerId ();


		Debug.Log ("Generating Nodes ...");



		// add anim nodes
		for (int i = 0; i < AnimObjects.Count; i++)
		{

			var ao = AnimObjects[i];

			// get needed ids
			string objName = ao.Name;
			string objId = fbxConn.searchObjectId(objName);

			string animCurveNodeT_id = getNewId();
			string animCurveNodeR_id = getNewId();
			string animCurveNodeS_id = getNewId();

			string curveT_X_id = getNewId();
			string curveT_Y_id = getNewId();
			string curveT_Z_id = getNewId();

			string curveR_X_id = getNewId();
			string curveR_Y_id = getNewId();
			string curveR_Z_id = getNewId();

			string curveS_X_id = getNewId();
			string curveS_Y_id = getNewId();
			string curveS_Z_id = getNewId();


			Debug.Log("Generating Node [" + objName + "]");


			// create Animation Curve Nodes
			fbxObj.AddAnimationCurveNode(animCurveNodeT_id, FbxAnimationCurveNodeType.Translation, ao.Frames[0].Position);
			fbxObj.AddAnimationCurveNode(animCurveNodeR_id, FbxAnimationCurveNodeType.Rotation, ao.Frames[0].RotationEular);
			fbxObj.AddAnimationCurveNode(animCurveNodeS_id, FbxAnimationCurveNodeType.Scale, ao.Frames[0].Scale);


			float[] xData;
			float[] yData;
			float[] zData;
			//int dataCount = objTracker.posDataList.Count;
			ao.GetPositionCurveData(out xData, out yData, out zData);

			fbxObj.AddAnimationCurve (curveT_X_id, xData);
			fbxObj.AddAnimationCurve (curveT_Y_id, yData);
			fbxObj.AddAnimationCurve (curveT_Z_id, zData);

			ao.GetRotationCurveData(out xData, out yData, out zData);
			fbxObj.AddAnimationCurve (curveR_X_id, xData);
			fbxObj.AddAnimationCurve (curveR_Y_id, yData);
			fbxObj.AddAnimationCurve (curveR_Z_id, zData);

			ao.GetScaleCurveData(out xData, out yData, out zData);
			fbxObj.AddAnimationCurve (curveS_X_id, xData);
			fbxObj.AddAnimationCurve (curveS_Y_id, yData);
			fbxObj.AddAnimationCurve (curveS_Z_id, zData);



			// setup connections
			fbxConn.AddConnectionItem ("AnimCurveNode", "T", animCurveNodeT_id, "Model", objName, objId, "OP", "Lcl Translation");
			fbxConn.AddConnectionItem ("AnimCurveNode", "R", animCurveNodeR_id, "Model", objName, objId, "OP", "Lcl Rotation");
			fbxConn.AddConnectionItem ("AnimCurveNode", "S", animCurveNodeS_id, "Model", objName, objId, "OP", "Lcl Scaling");

			fbxConn.AddConnectionItem ("AnimCurveNode", "T", animCurveNodeT_id, "AnimLayer", "BaseLayer", animBaseLayerId, "OO", "");
			fbxConn.AddConnectionItem ("AnimCurveNode", "R", animCurveNodeR_id, "AnimLayer", "BaseLayer", animBaseLayerId, "OO", "");
			fbxConn.AddConnectionItem ("AnimCurveNode", "S", animCurveNodeS_id, "AnimLayer", "BaseLayer", animBaseLayerId, "OO", "");

			fbxConn.AddConnectionItem ("AnimCurve", "", curveT_X_id, "AnimCurveNode", "T", animCurveNodeT_id, "OP", "d|X");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveT_Y_id, "AnimCurveNode", "T", animCurveNodeT_id, "OP", "d|Y");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveT_Z_id, "AnimCurveNode", "T", animCurveNodeT_id, "OP", "d|Z");

			fbxConn.AddConnectionItem ("AnimCurve", "", curveR_X_id, "AnimCurveNode", "R", animCurveNodeR_id, "OP", "d|X");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveR_Y_id, "AnimCurveNode", "R", animCurveNodeR_id, "OP", "d|Y");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveR_Z_id, "AnimCurveNode", "R", animCurveNodeR_id, "OP", "d|Z");

			fbxConn.AddConnectionItem ("AnimCurve", "", curveS_X_id, "AnimCurveNode", "S", animCurveNodeS_id, "OP", "d|X");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveS_Y_id, "AnimCurveNode", "S", animCurveNodeS_id, "OP", "d|Y");
			fbxConn.AddConnectionItem ("AnimCurve", "", curveS_Z_id, "AnimCurveNode", "S", animCurveNodeS_id, "OP", "d|Z");

		}

		Debug.Log ("Edit Defitions");
		ModifyDefinitions (exportFilePath);


		Debug.Log ("Edit Objects Data");

		// apply edition to file
		fbxObj.EditTargetFile (exportFilePath);

		Debug.Log ("Edit Connections Data");

		fbxConn.EditTargetFile (exportFilePath);


		// clear data
		fbxObj.objMainNode.clearSavedData();

		Debug.Log ("End Exporting");
	}
*/

	// generate ID
	int nowIdNum = 6000001;

	string getNewId ()
	{
		return (nowIdNum++).ToString ();
	}
}
