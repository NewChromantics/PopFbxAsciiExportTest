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


public class AnimFrame
{
	public Vector3 Position;
	public Quaternion Rotation;
	public Vector3 RotationEular{ get { return Rotation.eulerAngles; }}
	public Vector3 Scale;
	public float Time;
}

public class AnimObject
{
	public string Name;
	public List<AnimFrame> Frames;

	public void GetCurveData(out float[] x, out float[] y, out float[] z, System.Func<AnimFrame, float> GetX, System.Func<AnimFrame, float> GetY,System.Func<AnimFrame, float> GetZ)
	{
		x = new float[Frames.Count];
		y = new float[Frames.Count];
		z = new float[Frames.Count];
		for (int f = 0; f < Frames.Count; f++)
		{
			var Frame = Frames[f];
			x[f] = GetX(Frame);
			y[f] = GetY(Frame);
			z[f] = GetZ(Frame);
		}
	}
	public void GetPositionCurveData(out float[] x, out float[] y, out float[] z)
	{
		System.Func<AnimFrame, float> GetX = (Frame) => { return Frame.Position.x; };
		System.Func<AnimFrame, float> GetY = (Frame) => { return Frame.Position.y; };
		System.Func<AnimFrame, float> GetZ = (Frame) => { return Frame.Position.z; };
		GetCurveData(out x, out y, out z, GetX, GetY, GetZ);
	}

	public void GetRotationCurveData(out float[] x, out float[] y, out float[] z)
	{
		System.Func<AnimFrame, float> GetX = (Frame) => { return Frame.RotationEular.x; };
		System.Func<AnimFrame, float> GetY = (Frame) => { return Frame.RotationEular.y; };
		System.Func<AnimFrame, float> GetZ = (Frame) => { return Frame.RotationEular.z; };
		GetCurveData(out x, out y, out z, GetX, GetY, GetZ);
	}

	public void GetScaleCurveData(out float[] x, out float[] y, out float[] z)
	{
		System.Func<AnimFrame, float> GetX = (Frame) => { return Frame.Scale.x; };
		System.Func<AnimFrame, float> GetY = (Frame) => { return Frame.Scale.y; };
		System.Func<AnimFrame, float> GetZ = (Frame) => { return Frame.Scale.z; };
		GetCurveData(out x, out y, out z, GetX, GetY, GetZ);
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
	TransformTracker[] trackers;
	int objNums = -1;


	// record operation settings
	public KeyCode startRecordKey = KeyCode.Q;
	public KeyCode endRecordKey = KeyCode.W;

	// export settings
	bool includePathName = false;
	bool recordPos = true;
	bool recordRot = true;
	bool recordScale = true;



	// for recording
	bool isRecording = false;


	// Use this for initialization
	void Start ()
	{
		SetupRecordItems ();
	}

	void SetupRecordItems ()
	{
		
		// get all record objs
		observeTargets = gameObject.GetComponentsInChildren<Transform> ();
		trackers = new TransformTracker[ observeTargets.Length ];

		objNums = trackers.Length;

		for (int i = 0; i < objNums; i++) {

			string namePath = observeTargets [i].name;

			// if there are some nodes with same names, include path
			if (includePathName) {
				//namePath = AnimationRecorderHelper.GetTransformPathName (transform, observeTargets [i]);
				namePath = observeTargets [i].name;
				Debug.Log ("get name: " + namePath);
			}
			trackers [i] = new TransformTracker (observeTargets [i], recordPos, recordRot, recordScale);

		}
		Debug.Log ("setting complete");
	}


	
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetKeyDown (startRecordKey))
			StartRecording ();

		if (Input.GetKeyDown (endRecordKey))
			EndRecording ();
	}

	void StartRecording ()
	{
		isRecording = true;
		Debug.Log ("Start Recording");
	}

	void EndRecording ()
	{
		isRecording = false;
		Debug.Log ("End Recording");

		StartCoroutine (ExportToFile ());
	}

	void LateUpdate ()
	{
		if (isRecording) {
			for (int i = 0; i < trackers.Length; i++)
				trackers [i].recordFrame ();
		}
	}

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

	IEnumerator ExportToFile ()
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

		yield return null;





		Debug.Log ("fetch nodes ...");


		FbxDataNode[] allNodes = FbxDataNode.FetchNodes (File.ReadAllText (exportFilePath), 0);
		int objNodeIndex = 0;

		for (int i = 0; i < allNodes.Length; i++) {
			if (allNodes [i].nodeName == "Objects")
				objNodeIndex = i;
		}

		yield return null;

		// setup converter
		fbxObj = new FbxObjectsManager (allNodes[objNodeIndex], exportFileFolder);
		fbxConn = new FbxConnectionsManager (File.ReadAllText (exportFilePath));

		string animBaseLayerId = fbxConn.getAnimBaseLayerId ();


		Debug.Log ("Generating Nodes ...");

		List<AnimObject> AnimObjects;

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

			/*
			 * Create Objs
			 */

			// create Animation Curve Nodes
			fbxObj.AddAnimationCurveNode(animCurveNodeT_id, FbxAnimationCurveNodeType.Translation, observeTargets[i].localPosition);
			fbxObj.AddAnimationCurveNode(animCurveNodeR_id, FbxAnimationCurveNodeType.Rotation, observeTargets[i].localRotation.eulerAngles);
			fbxObj.AddAnimationCurveNode(animCurveNodeS_id, FbxAnimationCurveNodeType.Scale, observeTargets[i].localScale);


			float[] xData;
			float[] yData;
			float[] zData;
			//int dataCount = objTracker.posDataList.Count;
			ao.GetPositionCurveData(out xData, out yData, out zData);
			/*
			// create Curves
			// put in pos data
			for (int dataI = 0; dataI < dataCount; dataI++) {
				Vector3 mayaPos = objTracker.posDataList [dataI];
				xData [dataI] = mayaPos.x;
				yData [dataI] = mayaPos.y;
				zData [dataI] = mayaPos.z;
			}
			*/
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

			yield return null;
		}

		Debug.Log ("Edit Defitions");
		ModifyDefinitions (exportFilePath);
		yield return null;


		Debug.Log ("Edit Objects Data");

		// apply edition to file
		fbxObj.EditTargetFile (exportFilePath);
		yield return null;

		Debug.Log ("Edit Connections Data");

		fbxConn.EditTargetFile (exportFilePath);
		yield return null;


		// clear data
		fbxObj.objMainNode.clearSavedData();

		Debug.Log ("End Exporting");
	}


	// generate ID
	int nowIdNum = 6000001;

	string getNewId ()
	{
		return (nowIdNum++).ToString ();
	}
}
