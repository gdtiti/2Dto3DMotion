﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Assertions;
using Winterdust;
using System;

/** Helpfull links
 * BVH - Blender
 * https://forum.unity.com/threads/help-finishing-bvh-export-script-unity-animation-blender.127914/
 *  BVH - FBX orientations:
 *  https://forums.autodesk.com/t5/fbx-forum/eulerangles-quaternions-and-the-right-rotationorder/td-p/4166203
 *  Euler from matrix:
 *  https://stackoverflow.com/questions/32208838/rotation-matrix-to-quaternion-equivalence?rq=1
 *  https://stackoverflow.com/questions/1031005/is-there-an-algorithm-for-converting-quaternion-rotations-to-euler-angle-rotatio?rq=1
 *  https://threejs.org/examples/#webgl_loader_bvh
 *  bvh c# sdk
 *  https://neuronmocap.com/sites/default/files/downloads/neuron_datareader.pdf
 *  nice visuals
 *  http://lo-th.github.io/olympe/BVH_player.html
 *  
 * 1. To confirm the rotation order of your BVH file. In BVH file, if its channel like this:
CHANNELS 6 Xposition Yposition Zposition Xrotation Yrotation Zrotation
Then the transformations actually happened from the last one to the first one: Zrotation Yrotation Xrotation Zposition Yposition Xposition (rotate first, then translate)

2. Currently in FBX, all rotation are treated as in XYZ rotation order, for example, in the KFbxXMatrix, which is used to represent the affine transform, 
it only works in XYZ rotation order.
This limitation might be fixed in the future, but that's another thing and will not come soon.
 * 
 */

/* Important:
   Make sure that: the template, and all the bvh used in this program, have the same HIERARCHY. */

public class BvhExport
{
    private List<List<Rotations>> base_rotationFiles = Base.base_rotationFiles;

    private string HierarchyBody { get; set; }
    private string MotionBody { get; set; }
    private Neighbour[] Estimation { get; set; }
    private string TemplateFilename;
    private List<string> MotionLines { get; set; }
    public List<List<Vector3>> trianglesSource = new List<List<Vector3>>();
    public List<List<Vector3>> trianglesTarget = new List<List<Vector3>>();
    public BVH bvh_without_orientation;

    public BvhExport(string templateFilename, Neighbour[] solution)
    {
        TemplateFilename = templateFilename;
        HierarchyBody = FileToString(TemplateFilename);
        Estimation = solution;
    }




    private string FileToString(string TemplateFilename)
    {
        string text = System.IO.File.ReadAllText(TemplateFilename);
        return text;
    }

    /// <summary>
    /// TODO: Apply rotation and translation of root.
    /// </summary>
    /// <returns></returns>
    private List<string> CreateMotionLines(bool CorrectOrientation)
    {
        int counter = 0;
        List<string> lines = new List<string>();
        // T-Pose:
        // lines.Add("0 0 0 -0 90 90 9.69808e-018 1.34041e-020 0.158382 1.41245e-030 7.06225e-031 2.48481e-017 -7.30747e-017 7.61111e-019 -1.19349 3.40995e-017 1.65719e-019 0.556895 -2.12238e-008 -1.53545e-005 -90.1584 -2.50273e-005 -2.47192e-012 6.55612e-006 1.52588e-005 -6.98387e-015 174.455 180 -4.60993e-005 -89.8417 -5.49628e-005 -2.25992e-011 3.612e-005 1.52588e-005 6.98387e-015 174.455 -3.94791 -0.248796 -175.995 -8.56334 0.17593 -0.870008 4.60942 -0.0775208 176.868 177.984 -0.125371 -176.036 -4.37326 0.0836638 -0.877309 2.35438 -0.0332512 176.598\n");
        for(int frameIndex=0; frameIndex < Estimation.Length; frameIndex ++)
        {
            Neighbour n = Estimation[frameIndex];
            // If estimation in that frame does not exist.
            if (n == null)
                continue;

            List<Vector3> rotations = base_rotationFiles[n.projection.rotationFileID][n.projection.frameNum].getAllRotations();
            Vector3 defaultRootRotation = new Vector3(90, 90, 0); // Default. Please don't delete this. It is useful to debug.
            Vector3 rootRotation ;
            float angle = n.projection.angle;
            if (CorrectOrientation && trianglesSource[frameIndex]!=null && trianglesTarget[frameIndex]!=null)
            {
                //Quaternion rotationQuat = Triangle3DRot(trianglesSource[frameIndex], trianglesTarget[frameIndex]);
                //rootRotation = rotationQuat.eulerAngles;

                Quaternion initialQuat = bvh_without_orientation.allBones[0].localFrameRotations[frameIndex];
                bvh_without_orientation.rotateAnimationBy(Quaternion.Euler(0f, angle, 0f));       // apply angle
                Quaternion targetQuat = bvh_without_orientation.allBones[0].localFrameRotations[frameIndex];
                bvh_without_orientation.rotateAnimationBy(Quaternion.Euler(0f, -angle, 0f)); // reset angle
                Quaternion final = Quaternion.RotateTowards(initialQuat,targetQuat,180f);
                rootRotation = final.eulerAngles;
                //rootRotation = defaultRootRotation;
                
            }
            else
            {
                rootRotation = rotations[0];
            }
            
            lines.Add(CreateMLine(Vector3.zero, rootRotation, rotations));
            counter++;
        }
        return lines;
    }

    private Quaternion Triangle3DRot(List<Vector3> source, List<Vector3> target)
    {
        // https://stackoverflow.com/questions/11217680/how-to-calculate-the-quaternion-that-represents-a-triangles-3d-rotation
        // Define a triangle plane
        Vector3 s1, s2, s3, t1, t2, t3;
        s1 = source[0];
        s2 = source[1];
        s3 = source[2];

        // Yes, they are the same.
        // Debug.Log(s1+","+JointsGameObjects[(int)JointsDefinition.LeftUpLeg].position);

        t1 = target[0];
        t2 = target[1];
        t3 = target[2];
        // Calculations
        Vector3 normSource = Vector3.Cross((s1 - s2), (s1 - s3));
        Vector3 normTarget = Vector3.Cross((t1 - t2), (t1 - t3));
        Quaternion quat1 = Quaternion.FromToRotation(normSource, normTarget);
        Quaternion quat2 = Quaternion.FromToRotation(quat1 * (s1 - s2), (t1 - t2));
        Quaternion QuatFinal = quat2 * quat1;
        return QuatFinal;
    }


    private string CreateMLine(Vector3 rootPosition, Vector3 rootRotation, List<Vector3> rotations)
    {
        StringBuilder s = new StringBuilder();
        // Apply root's translation and rotation.
        s.Append(rootPosition.x + " " + rootPosition.y + " " + rootPosition.z + " ");
        s.Append(rootRotation.z + " " + rootRotation.x + " " + rootRotation.y + " ");

        for(int i=1; i<rotations.Count; i++) // start at 1 ==> skip rotation of the root.
        {
            s.Append(rotations[i].z + " " + rotations[i].x + " " + rotations[i].y + " ");
        }
        s.Append("\n");
        return s.ToString();
    }


   



    private string CreateMotionBody(bool CorrectOrientation)
    {
        MotionLines = CreateMotionLines(CorrectOrientation);
        StringBuilder s = new StringBuilder();
        s.Append("MOTION\nFrames: " + (MotionLines.Count) + "\nFrame Time: " + 0.0083333 + "\n");
        foreach (string line in MotionLines)
        {
            s.Append(line);
        }
        return s.ToString();
    }


    public void CreateBvhFile(string filename)
    {
        // StepA: Create a draft of the Motion, without the correct orientation.
        if (Estimation != null)
        {
            MotionBody = CreateMotionBody(false);
            System.IO.File.WriteAllText(filename/*+"_Without_Orientation.bvh"*/+".bvh", HierarchyBody + MotionBody);
            Debug.Log("Bvh without correct orientation has been exported.");
        }
        else
        {
            Debug.LogError("Couldn't export Bvh1.");
            return;
        }


        // StepB: Fix the orientation
        // correctOrientation(filename);
    }

    private void correctOrientation(string filename)
    {
        // 1. Read bvh
        bvh_without_orientation = new BVH(filename + "_Without_Orientation.bvh");
        // 2. Create Triangles Source
        for (int i = 0; i < bvh_without_orientation.frameCount; i++)
        {
            // Bvh (Source) Triangles
            Vector3[] positions = ProjectionManager.getPositionsfromBvh(bvh_without_orientation, i);
            List<Vector3> triangle = new List<Vector3>() {
                positions[(int)EnumJoint.RightUpLeg],
                positions[(int)EnumJoint.LeftUpLeg],
                positions[(int)EnumJoint.Spine1]
            };
            trianglesSource.Add(triangle);

            // Estimation (Target) Triangles.
            if (Estimation[i] != null)
            {
                positions = Estimation[i].projection.joints;
                triangle = new List<Vector3>() {
                positions[(int)EnumJoint.RightUpLeg],
                positions[(int)EnumJoint.LeftUpLeg],
                positions[(int)EnumJoint.Spine1]
                };
            }
            else
            {
                triangle = null;
            }
            trianglesTarget.Add(triangle);

        }
        // 3. Export final file
        MotionBody = CreateMotionBody(true);
        System.IO.File.WriteAllText(filename + ".bvh", HierarchyBody + MotionBody);
        Debug.Log("Bvh with correct orientation has been exported!!");
    }













    // >>


    public static Vector3 FromQ2(Quaternion q1)
    {
        float sqw = q1.w * q1.w;
        float sqx = q1.x * q1.x;
        float sqy = q1.y * q1.y;
        float sqz = q1.z * q1.z;
        float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
        float test = q1.x * q1.w - q1.y * q1.z;
        Vector3 v;

        if (test > 0.4995f * unit)
        { // singularity at north pole
            v.y = 2f * Mathf.Atan2(q1.y, q1.x);
            v.x = Mathf.PI / 2;
            v.z = 0;
            return NormalizeAngles(v * Mathf.Rad2Deg);
        }
        if (test < -0.4995f * unit)
        { // singularity at south pole
            v.y = -2f * Mathf.Atan2(q1.y, q1.x);
            v.x = -Mathf.PI / 2;
            v.z = 0;
            return NormalizeAngles(v * Mathf.Rad2Deg);
        }
        Quaternion q = new Quaternion(q1.w, q1.z, q1.x, q1.y);
        v.y = (float)Math.Atan2(2f * q.x * q.w + 2f * q.y * q.z, 1 - 2f * (q.z * q.z + q.w * q.w));     // Yaw
        v.x = (float)Math.Asin(2f * (q.x * q.z - q.w * q.y));                             // Pitch
        v.z = (float)Math.Atan2(2f * q.x * q.y + 2f * q.z * q.w, 1 - 2f * (q.y * q.y + q.z * q.z));      // Roll
        return NormalizeAngles(v * Mathf.Rad2Deg);
    }


    public static Quaternion Euler(float yaw, float pitch, float roll)
    {
        yaw *= Mathf.Deg2Rad;
        pitch *= Mathf.Deg2Rad;
        roll *= Mathf.Deg2Rad;

        double yawOver2 = yaw * 0.5f;
        float cosYawOver2 = (float)System.Math.Cos(yawOver2);
        float sinYawOver2 = (float)System.Math.Sin(yawOver2);
        double pitchOver2 = pitch * 0.5f;
        float cosPitchOver2 = (float)System.Math.Cos(pitchOver2);
        float sinPitchOver2 = (float)System.Math.Sin(pitchOver2);
        double rollOver2 = roll * 0.5f;
        float cosRollOver2 = (float)System.Math.Cos(rollOver2);
        float sinRollOver2 = (float)System.Math.Sin(rollOver2);
        Quaternion result;
        result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
        result.x = sinYawOver2 * cosPitchOver2 * cosRollOver2 + cosYawOver2 * sinPitchOver2 * sinRollOver2;
        result.y = cosYawOver2 * sinPitchOver2 * cosRollOver2 - sinYawOver2 * cosPitchOver2 * sinRollOver2;
        result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;

        return result;
    }

    static Vector3 NormalizeAngles(Vector3 angles)
    {
        angles.x = NormalizeAngle(angles.x);
        angles.y = NormalizeAngle(angles.y);
        angles.z = NormalizeAngle(angles.z);
        return angles;
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 360)
            angle -= 360;
        while (angle < 0)
            angle += 360;
        return angle;
    }


    // <<<
































    //private Matrix4x4 getRotationMatrix(float phi, float theta, float psi)
    //{
    //    float R11 = cos(theta) * cos(psi);
    //    float R12 = -cos(theta) * sin(psi);
    //    float R13 = sin(theta);
    //    float R21 = sin(psi) * cos(phi) + sin(theta) * cos(psi) * sin(phi);
    //    float R22 = cos(psi) * cos(phi) - sin(theta) * sin(psi) * sin(phi);
    //    float R23 = -cos(theta) * sin(phi);
    //    float R31 = sin(psi) * sin(phi) - sin(theta) * cos(psi) * cos(phi);
    //    float R32 = cos(psi) * sin(phi) + sin(theta) * sin(psi) * cos(phi);
    //    float R33 = cos(theta) * cos(phi);
    //    Matrix4x4 m = new Matrix4x4(new Vector4(R11, R12, R13), new Vector4(R21, R22, R23), new Vector4(R31, R32, R33), Vector4.zero);
    //    return m;
    //}

    //private float cos(float theta)
    //{
    //    return Mathf.Cos(theta);
    //}
    //private float sin(float theta)
    //{
    //    return Mathf.Sin(theta);
    //}

    //public static Vector3 getFinalRootRotationTEST(Vector3 rotationFromBvh, float angle = 0, int frameCounter = 0)
    //{
    //    Quaternion B = Quaternion.Euler(rotationFromBvh);
    //    GameObject g = new GameObject();
    //    g.transform.localRotation = B;
    //    g.transform.Rotate(0f, angle, 0f, Space.Self);
    //    Vector3 result = g.transform.rotation.eulerAngles;
    //    result = new Vector3(result.x, result.y, result.z);
    //    GameObject.DestroyImmediate(g);
    //    return To180(result);
    //}


    //// todo! :)
    //public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
    //{
    //    return Mathf.Atan2(
    //        Vector3.Dot(n, Vector3.Cross(v1, v2)),
    //        Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    //}


    private static float To180(float angle)
    {
        //while (angle > 180) angle -= 360;
        //while (angle < -180) angle += 360;
        
        angle = ((angle + 180) % 360) - 180;
       
        return angle;

    }

    private static Vector3 To180(Vector3 v)
    {
        return new Vector3(To180(v.x), To180(v.y), To180(v.z));
    }

    //private Vector3 getFinalRootRotation(Vector3 rotationFromBvh, float angle = 0, int frameCounter = 0)
    //{
    //    Quaternion qBefore;

    //    // [INITIALIZE]: Convert Euler to Quaternion for each axis. 
    //    //Quaternion qX = Quaternion.AngleAxis(rotationFromBvh.x, Vector3.right);
    //    //Quaternion qY = Quaternion.AngleAxis(rotationFromBvh.y, Vector3.up);
    //    //Quaternion qZ = Quaternion.AngleAxis(rotationFromBvh.z, Vector3.forward);
    //    //qBefore = qY * qX * qZ; // Multiply them in the rotation order, in case of ZXY.
    //    qBefore = Quaternion.Euler(rotationFromBvh);
    //    //qBefore.SetLookRotation(Vector3.down, Vector3.right); // << To make them look forward.
    //    //qBefore.SetLookRotation(Vector3.down, new Vector3(-rotationFromBvh.x,0f,0f));


    //    //Quaternion temp = qBefore;
    //    //temp.SetLookRotation();
    //    //Quaternion difference = Quaternion.Angle(qBefore);

    //    // Na ta kanw na vlepoun ola euthia
    //    // Na vrw tin diafora twn dio gwniwn (arxiki kai euthia)
    //    // Na peristrepsw + theta


    //    // [ROTATE]: Get the quaternion of rotating around y axis.
    //    Quaternion YawRotationQuaternion = Quaternion.AngleAxis(angle, Vector3.up);
    //    //qBefore.SetLookRotation(YawRotationQuaternion.eulerAngles); // <<

    //    // Make them Look Forward
    //    qBefore.SetLookRotation(Vector3.up, Vector3.right); // << To make them look forward.
    //    //Quaternion q = Quaternion.Euler(0, 0, 0);
    //    //q.SetLookRotation(Vector3.up, Vector3.right);
    //    //qBefore = qBefore * q;



    //    // [DEBUG ANGLE]
    //    //Vector3 debugangleaxis; float debugangle;
    //    //YawRotationQuaternion.ToAngleAxis(out debugangle, out debugangleaxis);
    //    //Debug.Log(angle + " >> ANGLE AXIS: "+ debugangleaxis + "debug angle: "+debugangle);

    //    // [APPLY ROTATION AROUND Y]
    //    Quaternion qAfter = qBefore;
    //    //qAfter = qBefore * YawRotationQuaternion;

    //    //Debug.Log("At frame: "+frameCounter+ " the angle is "+angle);
    //    /*
    //    // [EXTRA CALCULATIONS]:
    //    float signedAngle = GetSignedAngle(qBefore, qAfter, Vector3.up);
    //    // Angle axis of signed angle:
    //    Vector3 angleAxis;
    //    Quaternion x = (qAfter * Quaternion.Inverse(qBefore));
    //    x.ToAngleAxis(out angle, out angleAxis);
    //    // more
    //    Quaternion qInverseAfter = Quaternion.Inverse(qAfter);
    //    Quaternion qCenterBefore = Quaternion.Inverse(qBefore);
    //    float y_RotationAngle = Quaternion.Angle(qBefore, qAfter);
    //    float y_AngleBefore = qBefore.eulerAngles.y;
    //    float y_AngleAfter = qAfter.eulerAngles.y;
    //    */
    //    // [RETURN IN XYZ FORM]
    //    return qAfter.eulerAngles;
    //}


    //public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
    //{
    //    angle = angle * Mathf.Deg2Rad;
    //    Quaternion quaternion;
    //    float num2 = angle * 0.5f;
    //    float num = (float)Mathf.Sin((float)num2);
    //    float num3 = (float)Mathf.Cos((float)num2);
    //    quaternion.x = axis.x * num;
    //    quaternion.y = axis.y * num;
    //    quaternion.z = axis.z * num;
    //    quaternion.w = num3;
    //    return quaternion;

    //}


    //private static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
    //{
    //    Quaternion quaternion;
    //    double x = quaternion1.x;
    //    double y = quaternion1.y;
    //    double z = quaternion1.z;
    //    double w = quaternion1.w;
    //    double num4 = quaternion2.x;
    //    double num3 = quaternion2.y;
    //    double num2 = quaternion2.z;
    //    double num = quaternion2.w;
    //    double num12 = (y * num2) - (z * num3);
    //    double num11 = (z * num4) - (x * num2);
    //    double num10 = (x * num3) - (y * num4);
    //    double num9 = ((x * num4) + (y * num3)) + (z * num2);
    //    quaternion.x = (float)(((x * num) + (num4 * w)) + num12);
    //    quaternion.y = (float)(((y * num) + (num3 * w)) + num11);
    //    quaternion.z = (float)(((z * num) + (num2 * w)) + num10);
    //    quaternion.w = (float)((w * num) - num9);
    //    return quaternion;
    //}


    //public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis)
    //{
    //    float angle = 0f;
    //    Vector3 angleAxis = Vector3.zero;
    //    Quaternion x = (B * Quaternion.Inverse(A));
    //    x.ToAngleAxis(out angle, out angleAxis);
    //    if (Vector3.Angle(axis, angleAxis) > 90f)
    //    {
    //        angle = -angle;
    //    }
    //    // Debug.Log("Angle axis: "+angleAxis +"angle: "+angle+" singed angle: "+Mathf.DeltaAngle(0f,angle));
    //    return Mathf.DeltaAngle(0f, angle);
    //}



    //public void debugRotationsAxis()
    //{
    //    debugRotationsVersion(-1, -1, -1, "1.bvh");
    //    debugRotationsVersion(-1, -1,  1, "2.bvh");
    //    debugRotationsVersion(-1,  1, -1, "3.bvh");
    //    debugRotationsVersion(-1,  1,  1, "4.bvh");
    //    debugRotationsVersion( 1, -1, -1, "5.bvh");
    //    debugRotationsVersion( 1, -1,  1, "6.bvh");
    //    debugRotationsVersion( 1,  1, -1, "7.bvh");
    //    debugRotationsVersion( 1,  1,  1, "8.bvh");
    //}

    //private void debugRotationsVersion(int x, int y, int z, string filename)
    //{
    //    string s = HierarchyBody;
    //    s += "MOTION\nFrames: " + base_rotationFiles[0].Count + "\nFrame Time: " + 0.0083333 + "\n";
    //    foreach (Rotations r in base_rotationFiles[0])
    //    {
    //        s += "0 0 0 ";
    //        List<Vector3> v = r.getAllRotations();
    //        foreach (Vector3 eul in v)
    //        {
    //            s+= z*eul.z + " " + x*eul.x + " " + y*eul.y + " ";
    //        }
    //        s += "\n";
    //    }
    //    System.IO.File.WriteAllText(filename, s);
    //}


















}
