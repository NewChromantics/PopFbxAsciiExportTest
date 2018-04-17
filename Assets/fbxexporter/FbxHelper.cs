using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FbxHelper {

	public static long GetFbxSeconds ( int frameIndex, int frameRate ) {
		long result = 46186158000;
		result = result * frameIndex;
		result = result/frameRate;

		return result;
	}

	public static long GetFbxSeconds(float Time)
	{
		//	argh
		var KTimeSecondf = 46186158000.0f;
		var KTimef = Time * KTimeSecondf;
		var KTime = (long)KTimef;
		return KTime;
	}

}
