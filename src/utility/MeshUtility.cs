// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AsepriteShaderViewer {
    public static class MeshUtility {

        // Internal parser for face indices (v/vt/vn)
        private struct FaceInd {
            public int v;
            public int vt;
            public int vn;
            public int hash;

            public FaceInd(string raw) {
                string[] segments = raw.Split('/');

                int val;
                v =  segments.Length >= 1 && int.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? (val - 1) : 0;
                vt = segments.Length >= 2 && int.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? (val - 1) : 0;
                vn = segments.Length >= 3 && int.TryParse(segments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? (val - 1) : 0;

                hash = HashCode.Combine(v, vt, vn);
            }

            public override int GetHashCode() {
                return hash;
            }
        }

        // Internal parser for polygon slices in face index array
        private struct Face {
            public int offset;
            public int length;

            public int triCount => length - 2;

            public Face(int off, int len) {
                offset = off;
                length = len;
            }
        }

        /// <summary> Read an OBJ file and assign the vertices and indices to a mesh </summary>
        public static void AssignFromObj(this Mesh mesh, string raw) {            
            List<vec3> positions = new List<vec3>(1024);
            List<vec3> normals = new List<vec3>(1024);
            List<vec3> texcoords = new List<vec3>(1024);
            List<Face> faces = new List<Face>(1024);
            List<FaceInd> faceIndices = new List<FaceInd>(1024);            
            
            int offset = 0;     
            float subModelID = -1.0f;
            vec3 value;

            try { 
                using (StringReader reader = new StringReader(raw)) {
                    string line;
                    while((line = reader.ReadLine()) != null) {  
                        string[] elements = line.Split(' ');
                        if(elements == null || string.IsNullOrEmpty(elements[0])) continue;

                        // We ignore most blocks. all submeshes will be combined
                        switch (elements[0]) {
                            case "o":
                                subModelID += 1.0f;
                                break;

                            case "v":
                                value = WebMessage.ParseVec3(elements, 1);
                                positions.Add(value);
                                break;

                            case "vn":
                                value = WebMessage.ParseVec3(elements, 1);
                                normals.Add(value);
                                break;

                            case "vt":
                                value = WebMessage.ParseVec3(elements, 1);
                                value.Z = subModelID;
                                texcoords.Add(value);
                                break;

                            case "f":
                                for(int i = 1; i < elements.Length; i++) faceIndices.Add(new FaceInd(elements[i]));
                                faces.Add(new Face(offset, elements.Length - 1));
                                offset += elements.Length - 1;
     
                                break;
                        }                    
                    }                
                }

                // Create dictionary to filter out doubles
                Dictionary<FaceInd, int> indexFilter = new Dictionary<FaceInd, int>(faceIndices.Count);     
                foreach(FaceInd faceInd in faceIndices) {
                    if(!indexFilter.ContainsKey(faceInd)) indexFilter.Add(faceInd, indexFilter.Count);
                }

                // Read filtered indices and create vertices from them
                Vertex[] vertices = new Vertex[indexFilter.Count];

                foreach(KeyValuePair<FaceInd, int> pair in indexFilter) {
                    FaceInd faceInd = pair.Key;
                    vec3 v = positions.Count > faceInd.v ? positions[faceInd.v] : new vec3();
                    vec3 vt = texcoords.Count > faceInd.vt ? texcoords[faceInd.vt] : new vec3();
                    vec3 vn = normals.Count > faceInd.vn ? normals[faceInd.vn] : new vec3();

                    vertices[pair.Value] = new Vertex { x = v.X, y = v.Y, z = v.Z, nx = vn.X, ny = vn.Y, nz = vn.Z, u = vt.X, v = vt.Y, w = vt.Z };
                }

                // Create index array from calculated trianglecount
                int triangleCount = 0;
                foreach(Face face in faces) triangleCount += face.triCount;

                ushort[] indices = new ushort[triangleCount * 3];
                List<ushort> polygon = new List<ushort>(16);
                offset = 0;

                foreach(Face face in faces) {
                    // Remap read indices to filtered vertices
                    polygon.Clear();
                    for(int i = 0; i < face.length; i++) polygon.Add((ushort) indexFilter[faceIndices[face.offset + i]]);

                    // Read as triangle fan
                    for(int i = 0; i < face.triCount; i++) {
                        indices[offset++] = (ushort) polygon[0];
                        indices[offset++] = (ushort) polygon[i + 1];
                        indices[offset++] = (ushort) polygon[i + 2];
                    }
                }

                // Set results
                mesh.BindVAO();
                mesh.SetVertices(vertices);
                mesh.SetIndices(indices);

            } catch (Exception e) {
                Log.Print(e.Message, Log.MessageType.Error);
            }            
        }

        /// <summary> Assign a simple -1 to 1 unit quad to a mesh </summary>
        public static void AssignFromQuad(this Mesh mesh) {
            mesh.BindVAO();
            mesh.SetVertices(new Vertex[] {
                new Vertex(new vec3(-1.0f, -1.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f), new vec3(0.0f, 0.0f, 0.0f)),
                new Vertex(new vec3( 1.0f, -1.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f), new vec3(1.0f, 0.0f, 0.0f)),
                new Vertex(new vec3(-1.0f,  1.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f), new vec3(0.0f, 1.0f, 0.0f)),
                new Vertex(new vec3( 1.0f,  1.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f), new vec3(1.0f, 1.0f, 0.0f))
            });
            mesh.SetIndices(new ushort[] { 0, 1, 2, 3, 2, 1 });
        }

        /// <summary> Moller Trumbore intersection </summary>
        public static bool Intersect(Ray ray, Triangle tri, out RayHit hit) {
            hit = new RayHit();

            vec3 edge1 = tri.v1.Position - tri.v0.Position;
            vec3 edge2 = tri.v2.Position - tri.v0.Position;

            vec3 pvec = math.cross(ray.direction, edge2);

            float det = math.dot(edge1, pvec);

            // Parallel or backface
            if(det < float.Epsilon) return false;

            float invDet = 1.0f / det;

            vec3 tvec = ray.origin - tri.v0.Position;

            float u = math.dot(tvec, pvec) * invDet;
            if(u < 0.0f || u > 1.0f) return false;

            vec3 qvec = math.cross(tvec, edge1);

            float v = math.dot(ray.direction, qvec) * invDet;
            if (v < 0.0f || v > 1.0f) return false;

            float w = 1.0f - u - v;
            if(w < 0.0f || w > 1.0f) return false;

            // The uvw order for remapping has changed to wuv
            hit = new RayHit {
                position = tri.v0.Position * w + tri.v1.Position * u + tri.v2.Position * v,
                normal = tri.v0.Normal * w + tri.v1.Normal * u + tri.v2.Normal * v,
                uv = tri.v0.UV * w + tri.v1.UV * u + tri.v2.UV * v,
                distance = math.dot(edge2, qvec) * invDet
            };

            return hit.distance > float.Epsilon;
        }

        /// <summary> Raycast a mesh to get the uv </summary>
        public static bool Raycast(this Mesh mesh, Ray ray, out RayHit hit, float maxDistance = 100.0f) {
            hit = new RayHit();
            bool hasHit = false;
            
            int triCount = mesh.TriangleCount;

            // Parse through all triangles and do intersection tests to find the closest intersection
            for(int i = 0; i < triCount; i++) {
                Triangle tri = mesh.GetTriangle(i);

                if(Intersect(ray, tri, out RayHit localHit) && localHit.distance < maxDistance) {
                    hasHit = true;
                    maxDistance = localHit.distance;
                    hit = localHit;
                }
            }

            return hasHit;
        }

    }
}
