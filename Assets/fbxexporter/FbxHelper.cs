using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FbxHelper {

	public static string getFbxSeconds ( int frameIndex, int frameRate ) {
		long result = 46186158000;
		result = result * frameIndex;
		result = result/frameRate;

		return result.ToString ();
	}

}
