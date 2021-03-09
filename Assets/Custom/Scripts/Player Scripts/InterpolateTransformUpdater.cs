using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Credit To : Scott Sewell, developer at KinematicSoup
 * http://www.kinematicsoup.com/news/2016/8/9/rrypp5tkubynjwxhxjzd42s3o034o8
 */

public class InterpolateTransformUpdater : MonoBehaviour
{
    private InterpolateTransform m_interpolatedTransform;

    void Awake()
    {
        m_interpolatedTransform = GetComponent<InterpolateTransform>();
    }

    void FixedUpdate()
    {
        m_interpolatedTransform.LateFixedUpdate();
    }
}
