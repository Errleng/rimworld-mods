﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Jaxxa.EnhancedDevelopment.Shields.Shields.Utilities
{
    public static class Graphics
    {
        private static Mesh CircleMesh_cache;
        public static Mesh CircleMesh
        {
            get
            {

                if (CircleMesh_cache != null)
                    return CircleMesh_cache;
                else
                {
                    //Random radius
                    double radius = 1f;

                    //List of points in the circle
                    List<Vector3> list = new List<Vector3>();
                    list.Add(new Vector3(0f, 0f, 0f));
                    for (int i = 0; i <= 360; i += 4)
                    {
                        double num = i / 180f * Math.PI;
                        //Create the point
                        list.Add(new Vector3(0f, 0f, 0f)
                        {
                            x = (float)((double)radius * Math.Cos((double)num)),
                            y = (float)((double)radius * Math.Sin((double)num)),
                            z = 0
                        });
                    }
                    //Now convert the points in V3 array
                    Vector3[] array = new Vector3[list.Count];
                    for (int j = 0; j < array.Length; j++)
                    {
                        array[j] = new Vector3(list[j].x, 0f, list[j].y);
                    }

                    //Some magic
                    Triangulator triangulator = new Triangulator(list);
                    List<int> triangles = triangulator.Triangulate();
                    //Create the mesh object
                    CircleMesh_cache = new Mesh();
                    CircleMesh_cache.vertices = array;
                    CircleMesh_cache.uv = new Vector2[list.Count];
                    CircleMesh_cache.triangles = triangles.ToArray();
                    CircleMesh_cache.RecalculateNormals();
                    CircleMesh_cache.RecalculateBounds();
                    return CircleMesh_cache;
                }
            }
        }

    }
}
