﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Apply on camera object.
public class Visual_KBest2DFigures : MonoBehaviour
{

    public Material material;                       // The material used in gl lines.
    public DataInFrame showEstimationScript;        // Reference to the script which determines the selected pose to debug.
    public bool showBestOne;                        // Different color on the best one.
    public Color colorDefault;
    public Color colorBest;
    private OPPose figureToDebug;                    // The chosen figure to debug, from showEstimationScript.
    private GLDraw gL;                               // GL lines.
    private const int MaxFiguresPerLine = 5;         // Figures per line.
    public Vector3 offsetToCorner = new Vector3(20f,15f,0);              // We use this, so we can get to the up left corner of camera.
    public Vector3 offsetBetweenFigures = new Vector3(10f, 10f, 0);      // Space between figures.
    private float posX, posY;                                            // Location of a figure.
    private Color color;
    void Start()
    {
        gL = new GLDraw(material);
        resetPos();
    }

    void Update()
    {
        figureToDebug = showEstimationScript.selectedPoseToDebug;
    }

    private void OnPostRender()
    {
        if(figureToDebug!=null && figureToDebug.neighbours!=null)
        {
            // Render 1st figure's best k 2D match
            int neighCounter = 0;
            foreach (Neighbour neighbour in figureToDebug.neighbours)
            {
            
                if (neighCounter >= MaxFiguresPerLine)
                {
                    posY -= offsetBetweenFigures.y;
                    neighCounter = 0;
                    posX = transform.position.x - offsetToCorner.x;
                }

                // Here, we could put another color to the projections that had been selected as the leading one.
                // We could check if neighbour.projection is the same as figureToDebug.selectedN.projection
                Vector3 pos = new Vector2(posX, posY);
                color = colorDefault;
                if (showBestOne && neighbour.projection == figureToDebug.Estimation3D.projection)
                    color = colorBest;
                gL.drawFigure(true, color, neighbour.projection.joints, null, pos);
              
                posX += offsetBetweenFigures.x;
                neighCounter++;
            }
            resetPos();
        }

    }

    private void resetPos()
    {
        posX = transform.position.x - offsetToCorner.x;
        posY = transform.position.y + offsetToCorner.y;
    }

}
