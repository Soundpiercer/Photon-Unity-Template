using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UnityChan
{
	//[ExecuteInEditMode]
	public class SplashScreen : MonoBehaviour
	{
        // Executed by Animator
		private void NextLevel ()
		{
            // Application.LoadLevel(Application.loadedLevel + 1); Deprecated
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}
	}
}