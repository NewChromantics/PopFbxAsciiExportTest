using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif



public struct AnimFrame
{
	public Vector3 Position;
	public Quaternion Rotation;
	public Vector3 RotationEular{ get { return Rotation.eulerAngles; }}
	public Vector3 Scale;
	public float Time;
}

public class AnimObject
{
	public List<AnimFrame> Frames;

	public void AddFrame(Vector3 Position,Quaternion Rotation,float Time)
	{
		var Frame = new AnimFrame();
		Frame.Position = Position;
		Frame.Rotation = Rotation;
		Frame.Scale = Vector3.one;
		Frame.Time = Time;
		Pop.AllocIfNull(ref Frames);
		Frames.Add(Frame);
	}

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


//	rename namespace to Pop in later refactor
namespace PopX
{
	//	fbx is a tree of nodes, override this to generate the tree
	public class FbxProperty
	{
		//	Tag: value [value, value]
		public List<string>			Comments;
		public string				Name;
		public List<FbxValue>		Values;
		public List<FbxProperty>	Children;

		public FbxProperty(string Name)
		{
			this.Name = Name;
			this.Values = new List<FbxValue>();
			this.Comments = new List<string>();
		}

		//	add value
		public void AddValue(FbxValue Value)	{	Values.Add(Value);	}
		public void AddValue(string Value)		{	AddValue(FbxValues.Create(Value));	}
		public void AddValue(int Value)			{	AddValue(FbxValues.Create(Value));	}
		public void AddValue(float Value)		{	AddValue(FbxValues.Create(Value));	}


		//	add child property
		public FbxProperty AddProperty(string PropertyName, int Value) { return AddProperty(PropertyName, FbxValues.Create(Value)); }
		public FbxProperty AddProperty(string PropertyName, string Value) { return AddProperty(PropertyName, FbxValues.Create(Value)); }

		public FbxProperty AddProperty(string PropertyName, FbxValue Value)
		{
			var Prop = new FbxProperty(PropertyName);
			Prop.AddValue(Value);
			AddProperty(Prop);
			return Prop;
		}

		public FbxProperty AddProperty(string PropertyName)
		{
			var Prop = new FbxProperty(PropertyName);
			AddProperty(Prop);
			return Prop;
		}

		public void AddProperty(FbxProperty Child)
		{
			if (Children == null)
				Children = new List<FbxProperty>();
			Children.Add(Child);
		}

		public void AddComment(string Comment)
		{
			if (this.Comments == null)
				this.Comments = new List<string>();
			this.Comments.Add(Comment);
		}

		public void AddComment(List<string> Comments)
		{
			foreach (var Comment in Comments)
				AddComment(Comment);
		}
	};
		
	public interface FbxValue
	{
		string GetString();
	};

	public static class FbxValues
	{
		static public FbxValue Create(string Value) { return new FbxValue_String(Value); }
		static public FbxValue Create(int Value) { return new FbxValue_Ints(Value); }
		static public FbxValue Create(float Value) { return new FbxValue_Floats(Value); }
	};

	public struct FbxValue_Property : FbxValue
	{
		public FbxProperty Property;

		public string GetString()
		{
			throw new System.Exception("Don't call this on a property, export it via tree");
		}

		public FbxValue_Property(FbxProperty Property)
		{
			this.Property = Property;
		}
	};

	public struct FbxValue_Floats : FbxValue
	{
		public List<float> Numbers;

		static string GetString(float f)
		{
			return f.ToString("F3");
		}
		public string GetString()
		{
			var String = GetString(Numbers[0]);
			for (var i = 1; i < Numbers.Count; i++)
				String += FbxAscii.PropertySeperator + GetString(Numbers[i]);
			return String;
		}

		public FbxValue_Floats(float v) { Numbers = null; Append(v); }
		public FbxValue_Floats(float[] vs) { Numbers = null; foreach (var v in vs) Append(v); }
		public FbxValue_Floats(Vector3 v) { Numbers = null; Append(v); }
		public FbxValue_Floats(Vector3[] vs) { Numbers = null; foreach (var v in vs) Append(v); }
		public FbxValue_Floats(Vector3[] vs,System.Func<Vector3,Vector3> transform) { Numbers = null; foreach (var v in vs) Append(transform(v)); }

		void Append(Vector3 v)
		{
			Append(v.x);
			Append(v.y);
			Append(v.z);
		}

		void Append(float v)
		{
			if (Numbers == null)
				Numbers = new List<float>();
			Numbers.Add(v);
		}
	};

	public struct FbxValue_Ints : FbxValue
	{
		public List<long> Numbers;

		static string GetString(long f)
		{
			return f.ToString();
		}
		public string GetString()
		{
			var String = GetString(Numbers[0]);
			for (var i = 1; i < Numbers.Count; i++)
				String += FbxAscii.PropertySeperator + GetString(Numbers[i]);
			return String;
		}

		public FbxValue_Ints(int v) { Numbers = null; Append(v); }
		public FbxValue_Ints(int[] vs) { Numbers = null; foreach (var v in vs) Append(v); }
		public FbxValue_Ints(List<int> vs) { Numbers = null; foreach (var v in vs) Append(v); }
		public FbxValue_Ints(List<long> vs) { Numbers = null; foreach (var v in vs) Append(v); }

		void Append(long v)
		{
			if (Numbers == null)
				Numbers = new List<long>();
			Numbers.Add(v);
		}
	};

	public struct FbxValue_String : FbxValue
	{
		public string String;
		public string GetString() { return '"' + String+ '"'; }

		public FbxValue_String(string Value)
		{
			this.String = Value;
		}
	};

	public class AnimationCurve
	{
		
	}
	public class AnimationCurveNode
	{
		//	connect to property in other node
	}
	public class AnimationLayer
	{
		public List<AnimationCurveNode> AnimationCurveNodes;
	}
	public class AnimationStack
	{
		public string Name;
		public List<AnimationLayer> AnimationLayers;


	}
	// FBXTree.Objects.subNodes.AnimationCurve(connected to AnimationCurveNode )
	// FBXTree.Objects.subNodes.AnimationCurveNode ( connected to AnimationLayer and an animated property in some other node )
	// FBXTree.Objects.subNodes.AnimationLayer ( connected to AnimationStack )
	// FBXTree.Objects.subNodes.AnimationStack

	public static class FbxAscii
	{
		public const string FileExtension = "fbx";


		public const string Tag_Comment = "; ";
		public const string PropertySeperator = ", ";
		public const int Version = (VersionMajor * 1000) + (VersionMinor * 100) + (VersionRelease * 10);
		public const int VersionMajor = 6;
		public const int VersionMinor = 1;
		public const int VersionRelease = 0;
		public const bool ReversePolygonOrder = true;

		public const long KTime_Second = 46186158000;

		static FbxProperty GetHeaderProperty(string Creator = "Pop FbxAscii Exporter")
		{
			var Root = new FbxProperty("FBXHeaderExtension");
			Root.AddProperty("FBXHeaderVersion", 1003);
			Root.AddProperty("FBXVersion", Version);
			Root.AddProperty("Creator", Creator);

			//	won't load in unity without this comment at the top
			Root.AddComment("FBX " + VersionMajor + "." + VersionMinor + "." + VersionRelease.ToString("D2") + "  project file");
			return Root;
		}

		static List<int> GetMeshIndexes(int[] Indexes, MeshTopology Topology)
		{
			int PolyIndexCount;
			if (Topology == MeshTopology.Triangles)
				PolyIndexCount = 3;
			else if (Topology == MeshTopology.Quads)
				PolyIndexCount = 4;
			else
				throw new System.Exception("meshes of " + Topology + " are unsupported");

			var FbxIndexes = new List<int>(Indexes.Length);
			var Poly = new List<int>(new int[PolyIndexCount]);

			for (int i = 0; i < Indexes.Length; i += PolyIndexCount)
			{
				for (int p = 0; p < PolyIndexCount; p++)
					Poly[p] = Indexes[i + p];
				//	add in reverse order - imports with reverse winding it seems
				if (ReversePolygonOrder)
					Poly.Reverse();

				//	last one denotes end of polygon and is negative (+1)
				Poly[PolyIndexCount - 1] = -(Poly[PolyIndexCount - 1] + 1);
				FbxIndexes.AddRange(Poly);
			}
			return FbxIndexes;
		}

		static FbxObject CreateAnimLayerObject(FbxObjectManager ObjectManager)
		{
			var Object = ObjectManager.CreateObject("AnimationLayer");
			Object.Definition.AddValue(Object.Ident);
			Object.Definition.AddValue("AnimLayer::BaseLayer");
			Object.Definition.AddValue("");
			//AnimationLayer: 3445458688, , "AnimLayer::BaseLayer" {
			Object.Definition.AddProperty("Dummy");
			return Object;
		}

		static FbxObject CreateFbxObject(Mesh mesh, Matrix4x4 transform, FbxObjectManager ObjectManager)
		{
			var Object = ObjectManager.CreateObject(mesh.name);
			Object.Definition = new FbxProperty("Model");

			var Model = Object.Definition;
			//	gr: doesnt load in unity with an ident
			//Model.AddValue(Object.Ident);
			Model.AddValue("Model::" + mesh.name);
			Model.AddValue("Mesh");

			Model.AddProperty("Version", 232);
			Model.AddProperty("Vertices", new FbxValue_Floats(mesh.vertices, (n) => { return transform.MultiplyPoint(n); }));
			//	indexes start at 1, and last in poly is negative
			var FbxIndexes = GetMeshIndexes(mesh.GetIndices(0), mesh.GetTopology(0));
			Model.AddProperty("PolygonVertexIndex", new FbxValue_Ints(FbxIndexes));
			Model.AddProperty("GeometryVersion", 124);

			int LayerNumber = 0;
			var NormalLayer = Model.AddProperty("LayerElementNormal", LayerNumber);
			NormalLayer.AddProperty("Version", 101);
			NormalLayer.AddProperty("Name", "");
			//	ByPolygon	It means that there is a normal for every polygon of the model.
			//	ByPolygonVertex	It means that there is a normal for every vertex of every polygon of the model.
			//	ByVertex	It means that there is a normal for every vertex of the model.
			//	gr: ByVertex "Unsupported wedge mapping mode type.Please report this bug."
			//		even though I think that's the right one to use.. as ByPolygonVertex looks wrong
			NormalLayer.AddProperty("MappingInformationType", "ByPolygonVertex");
			NormalLayer.AddProperty("ReferenceInformationType", "Direct");
			NormalLayer.AddProperty("Normals", new FbxValue_Floats(mesh.normals, (n) => { return transform.MultiplyVector(n); }));

			var Layer = Model.AddProperty("Layer", LayerNumber);
			Layer.AddProperty("Version", 100);
			var len = Layer.AddProperty("LayerElement");
			len.AddProperty("Type", "LayerElementNormal");
			len.AddProperty("TypedIndex", 0);
			var les = Layer.AddProperty("LayerElement");
			les.AddProperty("Type", "LayerElementSmoothing");
			les.AddProperty("TypedIndex", 0);
			var leuv = Layer.AddProperty("LayerElement");
			leuv.AddProperty("Type", "LayerElementUV");
			leuv.AddProperty("TypedIndex", 0);
			var let = Layer.AddProperty("LayerElement");
			let.AddProperty("Type", "LayerElementTexture");
			let.AddProperty("TypedIndex", 0);
			var lem = Layer.AddProperty("LayerElement");
			lem.AddProperty("Type", "LayerElementMaterial");
			lem.AddProperty("TypedIndex", 0);

			return Object;
		}

		static FbxProperty GetDefinitionsProperty(int MeshCount)
		{
			var Defs = new FbxProperty("Definitions");
			Defs.AddProperty("Version", 100);
			Defs.AddProperty("Count", MeshCount);
			var otm = Defs.AddProperty("ObjectType", "Model");
			otm.AddProperty("Count", MeshCount);
			var otg = Defs.AddProperty("ObjectType", "Geometry");
			otg.AddProperty("Count", MeshCount);

			return Defs;
		}

		static FbxProperty GetConnectionsProperty(List<object[]> Connections)
		{
			var ConnectionsProp = new FbxProperty("Connections");

			foreach (var Connection in Connections)
			{
				var Prop = ConnectionsProp.AddProperty("Connect");
				foreach (var Value in Connection)
				{
					if (Value is string)
						Prop.AddValue((string)Value);
					if (Value is Mesh)
						Prop.AddValue("Model::" + ((Mesh)Value).name);
					if (Value is Material)
						Prop.AddValue("Material::" + ((Material)Value).name);
				}
			}

			return ConnectionsProp;
		}

		public static string GetIndent(int Indents)
		{
			var String = "";
			for (int i = 0; i < Indents; i++)
				String += "\t";
			return String;
		}

		public static void Export(System.Action<string> WriteLine, FbxProperty Property, int Indent = 0)
		{
			var IndentStr = GetIndent(Indent);
			foreach (var Comment in Property.Comments)
				WriteLine(IndentStr + Tag_Comment + Comment);

			var ValuesLine = IndentStr + Property.Name + ": ";
			var Values = Property.Values;

			for (int i = 0; i < Values.Count; i++)
			{
				var Value = Values[i];
				if (i > 0)
					ValuesLine += PropertySeperator;
				ValuesLine += Value.GetString();
			}

			WriteLine(ValuesLine);

			//	open tree
			if (Property.Children != null)
			{
				WriteLine(IndentStr + "{");
				foreach (var Child in Property.Children)
					Export(WriteLine, Child, Indent + 1);
				WriteLine(IndentStr + "}");
			}
		}

		public static void Export(System.Action<string> WriteLine, List<FbxProperty> Tree, List<string> Comments = null)
		{
			Pop.AllocIfNull(ref Comments);
			Comments.Add("Using WIP PopX.FbxAscii exporter from @soylentgraham");
			foreach (var Comment in Comments)
				WriteLine(Tag_Comment + " " + Comment);
			WriteLine(null);

			//	write out the tree
			foreach (var Prop in Tree)
			{
				Export(WriteLine, Prop);
				WriteLine(null);
			}
		}

		static void Export(System.Action<string> WriteLine, FbxObjectManager ObjectManager)
		{
			var ObjectPropertys = new FbxProperty("Objects");
			foreach (var Object in ObjectManager.Objects)
			{
				ObjectPropertys.AddProperty(Object.Definition);
			}
			Export(WriteLine, ObjectPropertys);
		}

		public static void Export(System.Action<string> WriteLine, Mesh mesh, Matrix4x4 transform, List<string> Comments = null)
		{
			Pop.AllocIfNull(ref Comments);
			Comments.Add("Using WIP PopX.FbxAscii exporter from @soylentgraham");

			var Header = GetHeaderProperty();
			Header.AddComment(Comments);
			Export(WriteLine, Header);

			var ConnectionManager = new FbxConnectionManager();
			var ObjectManager = new FbxObjectManager();
			var MeshObject = CreateFbxObject(mesh, transform, ObjectManager);
			var AnimLayer = CreateAnimLayerObject(ObjectManager);

			//	create anim
			var MeshAnim = new AnimObject();
			MeshAnim.AddFrame(Vector3.zero, Quaternion.identity, 0);
			MeshAnim.AddFrame(Vector3.one, Quaternion.identity, 1);
			MakeAnimationNode(MeshAnim, MeshObject, AnimLayer, ObjectManager, ConnectionManager);

			var SceneMesh = new Mesh();
			SceneMesh.name = "Scene";
			//	need a dummy material or it doesn't show up in unity
			var meshMaterial = "Material::CactusPack_Sprite";
			//var meshMaterial = new Material("Contents");
			//meshMaterial.name = "DummyMaterial";


			//	ConnectionManager
			var Connections = new List<object[]>();
			Connections.Add(new object[] { "OO", mesh, SceneMesh });
			Connections.Add(new object[] { "OO", meshMaterial, mesh });

			Export(WriteLine, ObjectManager);

			//	todo: get details from object manager
			var Definitions = GetDefinitionsProperty(1);
			Export(WriteLine, Definitions);


			var ConnectionsProp = GetConnectionsProperty(Connections);
			Export(WriteLine, ConnectionsProp);
		}


#if UNITY_EDITOR
		static List<string> GetSelectedObjFilenames()
		{
			var Filenames = new List<string>();
			var AssetGuids = Selection.assetGUIDs;
			for (int i = 0; i < AssetGuids.Length; i++)
			{
				var Guid = AssetGuids[i];
				var Path = AssetDatabase.GUIDToAssetPath(Guid);
				//	skip .
				var Ext = System.IO.Path.GetExtension(Path).Substring(1).ToLower();
				if (Ext != FileExtension)
					continue;

				Filenames.Add(Path);
			}
			return Filenames;
		}
#endif

		/*
#if UNITY_EDITOR
		[MenuItem("Assets/Camera/Export Fbx Ascii")]
		static void ExportFbx_Camera(MenuCommand menuCommand)
		{
			var cam = menuCommand.context as Camera;

			string Filename;
			var WriteLine = IO.GetFileWriteLineFunction(out Filename, "Fbx", cam.name, FileExtension);
			Export(WriteLine, cam, WavefrontObj.UnityToMayaTransform);
			EditorUtility.RevealInFinder(Filename);
		}
#endif
		*/

#if UNITY_EDITOR
		[MenuItem("CONTEXT/MeshFilter/Export Fbx Ascii")]
		static void ExportFbx_Mesh(MenuCommand menuCommand)
		{
			var mf = menuCommand.context as MeshFilter;
			var m = mf.sharedMesh;

			string Filename;
			var WriteLine = IO.GetFileWriteLineFunction(out Filename, "Fbx", m.name, FileExtension);
			Export(WriteLine, m, WavefrontObj.UnityToMayaTransform);
			EditorUtility.RevealInFinder(Filename);
		}
#endif


		class FbxObject
		{
			public int Ident;
			public FbxProperty Definition;  //	property that goes in objects
			public string TypeName	{ get { return Definition.Name; }}
			
			public FbxObject(int Ident,string TypeName)
			{
				this.Ident = Ident;
				this.Definition = new FbxProperty(TypeName);
			}
		}

		static string GetTypeString(FbxAnimationCurveNodeType Type)
		{
			switch (Type)
			{
				case FbxAnimationCurveNodeType.Translation: return "T";
				case FbxAnimationCurveNodeType.Rotation: return "R";
				case FbxAnimationCurveNodeType.Scale: return "S";
				case FbxAnimationCurveNodeType.Visibility: return "Visibility";
				default: throw new System.Exception("Unknown type " + Type);
			}
		}

		class FbxObjectManager
		{
			public List<FbxObject> Objects = new List<FbxObject>();
			int IdentCounter = 6000;

			int AllocIdent()
			{
				IdentCounter++;
				return IdentCounter;
			}

			public FbxObject CreateObject(string TypeName)
			{
				var Node = new FbxObject(AllocIdent(),TypeName);
				Objects.Add(Node);
				return Node;
			}



			public FbxObject AddAnimationCurveNode(FbxAnimationCurveNodeType NodeType,Vector3 DefaultValue)
			{
				var Node = new FbxObject(AllocIdent(),"AnimationCurveNode");
				Objects.Add(Node);

				//string nodeData = inputId + ", \"AnimCurveNode::" + curveTypeStr + "\", \"\"";
				//FbxDataNode animCurveNode = new FbxDataNode(nodeName, nodeData, 1);
				string CurveTypeStr = GetTypeString(NodeType);
				Node.Definition.AddValue(Node.Ident);
				Node.Definition.AddValue("AnimCurveNode::" + CurveTypeStr);
				Node.Definition.AddValue("");

				//FbxDataNode propertiesNode = new FbxDataNode("Properties70", "", 2);
				//animCurveNode.addSubNode(propertiesNode);
				var PropertiesNode = Node.Definition.AddProperty("Properties70");
				//propertiesNode.addSubNode(new FbxDataNode("P", "\"d|X\", \"Number\", \"\", \"A\"," + initData.x, 3));
				//propertiesNode.addSubNode(new FbxDataNode("P", "\"d|Y\", \"Number\", \"\", \"A\"," + initData.y, 3));
				//propertiesNode.addSubNode(new FbxDataNode("P", "\"d|Z\", \"Number\", \"\", \"A\"," + initData.z, 3));
				var px = PropertiesNode.AddProperty("P");
				px.AddValue("d|X");
				px.AddValue("Number");
				px.AddValue("");
				px.AddValue("A");
				px.AddValue(DefaultValue.x);

				var py = PropertiesNode.AddProperty("P");
				py.AddValue("d|Y");
				py.AddValue("Number");
				py.AddValue("");
				py.AddValue("A");
				py.AddValue(DefaultValue.y);


				var pz = PropertiesNode.AddProperty("P");
				pz.AddValue("d|Z");
				pz.AddValue("Number");
				pz.AddValue("");
				pz.AddValue("A");
				pz.AddValue(DefaultValue.z);

				// release memory
				//animCurveNode.saveDataOnDisk(saveFileFolder);
				//objMainNode.addSubNode(animCurveNode);


				return Node;
			}

			public FbxObject AddAnimationCurve(float[] curveData)
			{
				//	todo: use proper time!
				var TimeData = new List<long>();
				for (int i = 0; i < curveData.Length; i++)
				{
					TimeData.Add(FbxHelper.getFbxSeconds(i, 60));
				}

				//	add a new object
				var CurveNodeObj = new FbxObject(AllocIdent(), "AnimationCurve");
				Objects.Add(CurveNodeObj);
				var CurveNode = CurveNodeObj.Definition;
				CurveNode.AddValue(CurveNodeObj.Ident);
				CurveNode.AddValue("AnimCurve::");	//	name
				CurveNode.AddValue("");

				//AnimationCurve: 106102887970656, "AnimCurve::", "" 
				//string nodeData = inputId + ", \"AnimCurve::\", \"\"";
				//FbxDataNode curveNode = new FbxDataNode("AnimationCurve", nodeData, 1);


				CurveNode.AddProperty("Default", 0);
				CurveNode.AddProperty("KeyVer", 4008);

				var keyTimeNode = CurveNode.AddProperty("KeyTime");
				keyTimeNode.AddValue("*" + curveData.Length);
				keyTimeNode.AddProperty("a",new FbxValue_Ints(TimeData));
				//FbxDataNode keyTimeNode = new FbxDataNode("KeyTime", "*" + dataLengthStr, 2);
				//keyTimeNode.addSubNode("a", timeArrayDataStr);
				//curveNode.addSubNode(keyTimeNode);

				var keyValuesNode = CurveNode.AddProperty("KeyValueFloat");
				keyValuesNode.AddValue("*" + curveData.Length);
				keyValuesNode.AddProperty("a",new FbxValue_Floats(curveData));
				//var keyValuesNode = new FbxDataNode("KeyValueFloat", "*" + dataLengthStr, 2);
				//keyValuesNode.addSubNode("a", keyValueFloatDataStr);
				//curveNode.addSubNode(keyValuesNode);

				//curveNode.addSubNode(";KeyAttrFlags", "Cubic|TangeantAuto|GenericTimeIndependent|GenericClampProgressive");
				var keyAttrFlagsNode = CurveNode.AddProperty("KeyAttrFlags");
				keyAttrFlagsNode.AddComment("KeyAttrFlags = Cubic | TangeantAuto | GenericTimeIndependent | GenericClampProgressive");
				//FbxDataNode keyAttrFlagsNode = new FbxDataNode("KeyAttrFlags", "*1", 2);
				keyAttrFlagsNode.AddValue("*1");
				keyAttrFlagsNode.AddProperty("a", "24840");
				//curveNode.addSubNode(keyAttrFlagsNode);

				var keyRefCountNode = CurveNode.AddProperty("KeyAttrRefCount");
				keyRefCountNode.AddValue("*1");
				//FbxDataNode keyRefCountNode = new FbxDataNode("KeyAttrRefCount", "*1", 2);
				keyRefCountNode.AddProperty("a", curveData.Length);
				//keyRefCountNode.addSubNode("a", dataLengthStr);
				//curveNode.addSubNode(keyRefCountNode);

				//	objects.add curvenode
				//return curveNode;
			
				return CurveNodeObj;
			}
		}

		class FbxConnectionManager
		{
			public List<FbxConnection> Connections = new List<FbxConnection>();

			public void Add(FbxConnection Connection)
			{
				Connections.Add(Connection);
			}
		}


		struct FbxConnection
		{
			/*
			 * ;AnimCurveNode::T, Model::pCube1
			 * C: "OP",105553124109952,140364338281984, "Lcl Translation"
			 * 
			 */

			public string type1;
			public string name1;
			public FbxObject Object1;

			public string type2;
			public string name2;
			public FbxObject Object2;

			public string relation;
			public string Comment;

			public FbxConnection(string type1,string name1,FbxObject Object1, string type2,string name2,FbxObject Object2, string relation,string Comment)
			{
				this.type1 = type1;
				this.name1 = name1;
				this.Object1 = Object1;

				this.type2 = type2;
				this.name2 = name2;
				this.Object2 = Object2;

				this.relation = relation;
				this.Comment = Comment;
			}
			/*
			public string getOutputData()
			{
				if (hasRelationDesc)
					return "\t;" + type1 + "::" + name1 + ", " + type2 + "::" + name2 + "\n\tC: \"" + relation + "\"," + id1 + "," + id2 + ", \"" + relationDesc + "\"\n";
				else
					return "\t;" + type1 + "::" + name1 + ", " + type2 + "::" + name2 + "\n\tC: \"" + relation + "\"," + id1 + "," + id2 + "\n";
			}
*/
		}

		static void MakeAnimationNode(AnimObject Anim,FbxObject AnimatedObject,FbxObject AnimLayer,FbxObjectManager ObjectManager, FbxConnectionManager ConnectionManager)
		{
			//	make the animation nodes
			FbxObject AnimNodePosition, AnimNodeRotation, AnimNodeScale;
			MakeAnimationNode(Anim, AnimLayer, ObjectManager, ConnectionManager, out AnimNodePosition, out AnimNodeRotation, out AnimNodeScale);

			//	object connection
			ConnectionManager.Add( new FbxConnection("AnimCurveNode", "T", AnimNodePosition, "Model", AnimatedObject.TypeName, AnimatedObject, "OP", "Lcl Translation"));
			ConnectionManager.Add( new FbxConnection("AnimCurveNode", "R", AnimNodeRotation, "Model", AnimatedObject.TypeName, AnimatedObject, "OP", "Lcl Rotation"));
			ConnectionManager.Add( new FbxConnection("AnimCurveNode", "S", AnimNodeScale, "Model", AnimatedObject.TypeName, AnimatedObject, "OP", "Lcl Scaling"));
		}


		static void MakeAnimationNode(AnimObject Anim,FbxObject AnimLayer,FbxObjectManager ObjectManager,FbxConnectionManager ConnectionManager, out FbxObject AnimNodePosition, out FbxObject AnimNodeRotation, out FbxObject AnimNodeScale)
		{
			// add anim nodes
			var ao = Anim;

			// create Animation Curve Nodes
			var NodeT = ObjectManager.AddAnimationCurveNode(FbxAnimationCurveNodeType.Translation, ao.Frames[0].Position);
			var NodeR = ObjectManager.AddAnimationCurveNode(FbxAnimationCurveNodeType.Rotation, ao.Frames[0].RotationEular);
			var NodeS = ObjectManager.AddAnimationCurveNode(FbxAnimationCurveNodeType.Scale, ao.Frames[0].Scale);

			AnimNodePosition = NodeT;
			AnimNodeRotation = NodeR;
			AnimNodeScale = NodeS;

			//	get data
			float[] TXData;
			float[] TYData;
			float[] TZData;
			ao.GetPositionCurveData(out TXData, out TYData, out TZData);
			var CurveTX = ObjectManager.AddAnimationCurve(TXData);
			var CurveTY = ObjectManager.AddAnimationCurve(TYData);
			var CurveTZ = ObjectManager.AddAnimationCurve(TZData);

			float[] RXData;
			float[] RYData;
			float[] RZData;
			ao.GetRotationCurveData(out RXData, out RYData, out RZData);
			var CurveRX = ObjectManager.AddAnimationCurve(RXData);
			var CurveRY = ObjectManager.AddAnimationCurve(RYData);
			var CurveRZ = ObjectManager.AddAnimationCurve(RZData);

			float[] SXData;
			float[] SYData;
			float[] SZData;
			ao.GetScaleCurveData(out SXData, out SYData, out SZData);
			var CurveSX = ObjectManager.AddAnimationCurve(SXData);
			var CurveSY = ObjectManager.AddAnimationCurve(SYData);
			var CurveSZ = ObjectManager.AddAnimationCurve(SZData);



			//	animation 
			ConnectionManager.Add(new FbxConnection("AnimCurveNode", "T", NodeT, "AnimLayer", "BaseLayer", AnimLayer, "OO", ""));
			ConnectionManager.Add(new FbxConnection("AnimCurveNode", "R", NodeR, "AnimLayer", "BaseLayer", AnimLayer, "OO", ""));
			ConnectionManager.Add(new FbxConnection("AnimCurveNode", "S", NodeS, "AnimLayer", "BaseLayer", AnimLayer, "OO", ""));

			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveTX, "AnimCurveNode", "T", NodeT, "OP", "d|X"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveTY, "AnimCurveNode", "T", NodeT, "OP", "d|Y"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveTZ, "AnimCurveNode", "T", NodeT, "OP", "d|Z"));

			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveRX, "AnimCurveNode", "R", NodeR, "OP", "d|X"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveRY, "AnimCurveNode", "R", NodeR, "OP", "d|Y"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveRZ, "AnimCurveNode", "R", NodeR, "OP", "d|Z"));

			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveSX, "AnimCurveNode", "S", NodeS, "OP", "d|X"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveSY, "AnimCurveNode", "S", NodeS, "OP", "d|Y"));
			ConnectionManager.Add(new FbxConnection("AnimCurve", "", CurveSZ, "AnimCurveNode", "S", NodeS, "OP", "d|Z"));

		}


	}
}


