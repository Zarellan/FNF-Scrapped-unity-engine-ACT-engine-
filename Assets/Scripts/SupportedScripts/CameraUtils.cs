using UnityEngine;

public static class CameraUtils
{
    public static GameObject GetCamera()
    {
        return GameObject.FindWithTag("MainCamera");
    }

    static Vector3 velocity = Vector3.zero;
    static Transform lastTarget;
    public static void CamFollow(Transform transform,Vector3 offset = default, Vector3 min = default,Vector3 max = default)
    {
        if (min == default)
            min = new Vector3(-99999, -99999, -99999);
        if (max == default)
            max = new Vector3(99999, 99999, 99999);
        
        if (transform == null) return;

        if (transform != lastTarget)
        {
            velocity = Vector3.zero;
            lastTarget = transform;
        }

        Vector3 target = new Vector3(Mathf.Clamp(transform.position.x, min.x, max.x), Mathf.Clamp(transform.position.y, min.x, max.y),Mathf.Clamp(transform.position.z, min.z, max.z));

        Vector3 mCam = GameObject.FindWithTag("MainCamera").transform.position;

        GameObject.FindWithTag("MainCamera").transform.position = Vector3.SmoothDamp(mCam, target + offset, ref velocity, 0.25f);

    }

}
