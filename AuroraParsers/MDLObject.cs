using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using GlmNet;
using System.ComponentModel;
using System.Drawing;

namespace KotOR_Files.AuroraParsers
{
    public class MDLObject
    {

        static int kNodeFlagHasHeader = 0x0001;
        static int kNodeFlagHasLight = 0x0002;
        static int kNodeFlagHasEmitter = 0x0004;
        static int kNodeFlagHasReference = 0x0010;
        static int kNodeFlagHasMesh = 0x0020;
        static int kNodeFlagHasSkin = 0x0040;
        static int kNodeFlagHasAnim = 0x0080;
        static int kNodeFlagHasDangly = 0x0100;
        static int kNodeFlagHasAABB = 0x0200;
        static int kNodeFlagHasSaber = 0x0800; //2081

        static int kClassFlagEffect = 0x01;
        static int kClassFlagTile = 0x02;
        static int kClassFlagCharacter = 0x04;
        static int kClassFlagDoor = 0x08;
        static int kClassFlagPlaceable = 0x20;
        static int kClassFlagOther = 0x00;

        private AuroraFile mdl;
        private AuroraFile mdx;
        private BinaryReader mdlReader;
        private BinaryReader mdxReader;
        private List<MeshNode> meshes = new List<MeshNode>();


        private FileHeader fileHeader = new FileHeader();
        private GeometryHeader geometryHeader = new GeometryHeader();
        private ModelHeader modelHeader = new ModelHeader();

        public List<NodeHeader> nodes = new List<NodeHeader>();

        private string[] names;


        private vec3 position = new vec3(0f);
        private vec3 scale = new vec3(1.5f);
        private float rotation = 0f;

        public MDLObject(AuroraFile mdl, AuroraFile mdx)
        {
            this.mdl = mdl;
            this.mdx = mdx;
        }


        public void getBoundingBox()
        {
            /*
            float[] boundingMin = new float[3], boundingMax = new float[3];

            boundingMin[0] = mdlReader.ReadSingle();
            boundingMin[1] = mdlReader.ReadSingle();
            boundingMin[2] = mdlReader.ReadSingle();

            boundingMax[0] = mdlReader.ReadSingle();
            boundingMax[1] = mdlReader.ReadSingle();
            boundingMax[2] = mdlReader.ReadSingle();
            */
        }

        public void Read()
        {

            mdl.Open();
            mdlReader = mdl.getReader();

            mdx.Open();
            mdxReader = mdx.getReader();

            /*
             * File Header
             */

            fileHeader.FlagBinary = mdlReader.ReadUInt32();

            if (fileHeader.FlagBinary != 0)
                throw new Exception("Unsupported KotOR ASCII MDL");

            fileHeader.ModelDataSize = mdlReader.ReadUInt32();
            fileHeader.RawDataSize = mdlReader.ReadUInt32();

            fileHeader.ModelDataOffset = 12;
            fileHeader.RawDataOffset = fileHeader.ModelDataOffset + fileHeader.ModelDataSize;

            /*
             * Geometry Header
             */

            geometryHeader.Unknown1 = mdlReader.ReadUInt32(); //4Byte Function pointer
            geometryHeader.Unknown2 = mdlReader.ReadUInt32(); //4Byte Function pointer

            geometryHeader.ModelName = mdlReader.ReadChars(32);
            geometryHeader.RootNodeOffset = mdlReader.ReadUInt32();
            geometryHeader.NodeCount = mdlReader.ReadUInt32();
            geometryHeader.Unknown3 = mdlReader.ReadBytes(24 + 4); // Unknown + Reference count
            geometryHeader.GeometryType = mdlReader.ReadSByte(); //Model Type

            geometryHeader.Unknown4 = mdlReader.ReadBytes(3); //Padding

            /*
             * Model Header
             */

            modelHeader.Unknown1 = mdlReader.ReadBytes(2); //Unknown
            modelHeader.Classification = mdlReader.ReadSByte();
            modelHeader.Fogged = mdlReader.ReadSByte();
            modelHeader.Unknown2 = mdlReader.ReadBytes(4); //Unkown

            AuroraFile.ReadArrayDef(mdlReader, out modelHeader.AnimationDataOffset, out modelHeader.AnimationsCount);
            modelHeader.AnimationsAllocated = modelHeader.AnimationsCount;

            modelHeader.ParentModelPointer = mdlReader.ReadUInt32(); // Parent model pointer

            modelHeader.BoundingMinX = mdlReader.ReadSingle();
            modelHeader.BoundingMinY = mdlReader.ReadSingle();
            modelHeader.BoundingMinZ = mdlReader.ReadSingle();
            modelHeader.BoundingMaxX = mdlReader.ReadSingle();
            modelHeader.BoundingMaxY = mdlReader.ReadSingle();
            modelHeader.BoundingMaxZ = mdlReader.ReadSingle();
            modelHeader.Radius = mdlReader.ReadSingle();
            modelHeader.Scale = mdlReader.ReadSingle();
            modelHeader.SuperModelName = mdlReader.ReadChars(32);

            /*
             * Names Array Header
             */

            mdlReader.BaseStream.Position += 4; // Root node pointer again

            mdlReader.BaseStream.Position += 12; // Unknown

            UInt32 nameOffset, nameCount;
            AuroraFile.ReadArrayDef(mdlReader, out nameOffset, out nameCount);

            UInt32[] nameOffsets = new UInt32[nameCount];
            AuroraFile.ReadArray(mdlReader, fileHeader.ModelDataOffset + nameOffset, nameCount, ref nameOffsets);

            //System.Diagnostics.Debug.WriteLine("Strings Count: " + nameCount);

            AuroraFile.readStrings(mdlReader, nameOffsets, fileHeader.ModelDataOffset, out names);

            /*
             * Animation Header
             */




            //START: TEST - Loading Root Node
            long tmpPos = mdlReader.BaseStream.Position;
            long nodeOffset = fileHeader.ModelDataOffset + geometryHeader.RootNodeOffset;

            loadModelNode(nodeOffset);

            mdlReader.BaseStream.Position = tmpPos;
            //END:   TEST - Loading Root Node


            UInt32[] animOffsets = new UInt32[modelHeader.AnimationsCount];
            AuroraFile.ReadArray(mdlReader, fileHeader.ModelDataOffset + modelHeader.AnimationDataOffset, modelHeader.AnimationsCount, ref animOffsets);

            for (int i = 0; i!=modelHeader.AnimationsCount; i++)
            {
                //newState(ctx);
                UInt32 offset = animOffsets[i];
                //readAnim(ctx, fileHeader.ModelDataOffset + offset);

                //addState(ctx);
            }


            Debug.WriteLine("model name: " + new string(geometryHeader.ModelName));
            Debug.WriteLine("SuperModel name: " + new string(modelHeader.SuperModelName));

            Debug.WriteLine("Root Node Offset: " + geometryHeader.RootNodeOffset);
            Debug.WriteLine("Node Count: " + geometryHeader.NodeCount);

            mdl.Close();
            mdx.Close();

        }

        private NodeHeader loadModelNode(long offset)
        {
            
            NodeHeader node = new NodeHeader();
            nodes.Add(node);
            mdlReader.BaseStream.Position = offset;

            node.NodeType = mdlReader.ReadUInt16();
            node.Supernode = mdlReader.ReadUInt16();
            node.NodePosition = mdlReader.ReadUInt16();
            

            if (node.NodePosition < names.Length)
                node.Name = names[node.NodePosition];


            // 2 4 4

            node.Unknown = mdlReader.ReadUInt16();
            node.Unknown2 = mdlReader.ReadBytes(4);

            node.ParentNodeLoc = mdlReader.ReadUInt32(); //Parent node pointer

            node.PositionX = mdlReader.ReadSingle();
            node.PositionY = mdlReader.ReadSingle();
            node.PositionZ = mdlReader.ReadSingle();

            node.position = new vec3(node.PositionX, node.PositionY, node.PositionZ);

            node.RotationW = (float)Utility.RadianToDegree(Math.Acos(mdlReader.ReadSingle()) * 2.0f);
            node.RotationX = mdlReader.ReadSingle();
            node.RotationY = mdlReader.ReadSingle();
            node.RotationZ = mdlReader.ReadSingle();

            //Debug.WriteLine(node.RotationW + " : " + node.RotationX + " : " + node.RotationY + " : " + node.RotationZ);

            AuroraFile.ReadArrayDef(mdlReader, out node.ChildNodesOffset, out node.ChildNodesCount);

            UInt32[] children = new UInt32[node.ChildNodesCount];
            AuroraFile.ReadArray(mdlReader, fileHeader.ModelDataOffset + node.ChildNodesOffset, node.ChildNodesCount, ref children);

            AuroraFile.ReadArrayDef(mdlReader, out node.ControllerKeyOffset, out node.ControllerKeyCount);

            AuroraFile.ReadArrayDef(mdlReader, out node.ControllerDataOffset, out node.ControllerDataCount);

            float[] controllerData = new float[node.ControllerDataCount];
            AuroraFile.ReadArray(mdlReader, fileHeader.ModelDataOffset + node.ControllerDataOffset, node.ControllerDataCount, ref controllerData);

            //readNodeControllers(mdlReader, fileHeader.ModelDataOffset + node.ControllerKeyOffset, node.ControllerKeyCount, controllerData);

            Debug.WriteLine("Name: "+ node.Name+ " +Node Type: " + node.NodeType + " " + kClassFlagPlaceable);

                //if ((node.NodeType & 0xFC00) != 0)
                //throw new Exception("Unknown node flags " + node.NodeType);

            if ((node.NodeType & kNodeFlagHasLight) == kNodeFlagHasLight)
            {
                // TODO: Light
                //ctx.mdl->skip(0x5C);
                mdlReader.BaseStream.Position += 0x5C;
            }

            if ((node.NodeType & kNodeFlagHasEmitter) == kNodeFlagHasEmitter)
            {
                // TODO: Emitter
                //ctx.mdl->skip(0xD8);
                mdlReader.BaseStream.Position += 0xD8;
            }

            if ((node.NodeType & kNodeFlagHasReference) == kNodeFlagHasReference)
            {
                // TODO: Reference
                //ctx.mdl->skip(0x44);
                mdlReader.BaseStream.Position += 0x44;
            }

            if ((node.NodeType & kNodeFlagHasMesh) == kNodeFlagHasMesh)
            {
                ReadMesh(node);
            }

            if ((node.NodeType & kNodeFlagHasSkin) == kNodeFlagHasSkin)
            {
                // TODO: Skin
                //ctx.mdl->skip(0x64);
                mdlReader.BaseStream.Position += 0x64;
            }

            if ((node.NodeType & kNodeFlagHasAnim) == kNodeFlagHasAnim)
            {
                // TODO: Anim
                //ctx.mdl->skip(0x38);
                mdlReader.BaseStream.Position += 0x38;
            }

            if ((node.NodeType & kNodeFlagHasDangly) == kNodeFlagHasDangly)
            {
                // TODO: Dangly
                //ctx.mdl->skip(0x18);
                mdlReader.BaseStream.Position += 0x18;
            }

            if ((node.NodeType & kNodeFlagHasAABB) == kNodeFlagHasAABB)
            {
                // TODO: AABB
                //ctx.mdl->skip(0x4);
                mdlReader.BaseStream.Position += 0x4;
            }

            for (int i = 0; i != children.Length; i++)
            {

                long nodeOffset = fileHeader.ModelDataOffset + children[i];
                //Debug.WriteLine("Child Offset: " + nodeOffset);
                NodeHeader child = loadModelNode(nodeOffset);
                child.setParent(node);

                //child.position = child.position + node.position;
                
                
            }
            return node;
        }

        private void ReadMesh(NodeHeader parent)
        {
            MeshNode mesh = new MeshNode();
            mesh.setParent(parent);
            
            //mesh.Unknown1 = mdlReader.ReadUInt32(); //Function Pointer
            //mesh.Unknown2 = mdlReader.ReadUInt32(); //Function Pointer

            mdlReader.BaseStream.Position += 8;

            AuroraFile.ReadArrayDef(mdlReader, out mesh.FaceArrayOffset, out mesh.FaceArrayCount);
            

            mesh.BoundingBoxMinX = mdlReader.ReadSingle();
            mesh.BoundingBoxMinY = mdlReader.ReadSingle();
            mesh.BoundingBoxMinZ = mdlReader.ReadSingle();

            mesh.BoundingBoxMaxX = mdlReader.ReadSingle();
            mesh.BoundingBoxMaxY = mdlReader.ReadSingle();
            mesh.BoundingBoxMaxZ = mdlReader.ReadSingle();

            mesh.Radius = mdlReader.ReadSingle();

            mesh.PointsAverageX = mdlReader.ReadSingle();
            mesh.PointsAverageY = mdlReader.ReadSingle();
            mesh.PointsAverageZ = mdlReader.ReadSingle();

            mesh.DiffuseR = mdlReader.ReadSingle();
            mesh.DiffuseG = mdlReader.ReadSingle();
            mesh.DiffuseB = mdlReader.ReadSingle();

            mesh.AmbientR = mdlReader.ReadSingle();
            mesh.AmbientG = mdlReader.ReadSingle();
            mesh.AmbientB = mdlReader.ReadSingle();

            mesh.TransparencyHint = mdlReader.ReadUInt32();

            bool _hasTransparencyHint = true;
            bool _transparencyHint = (mesh.TransparencyHint != 0);

            mesh.TextureMap1 = mdlReader.ReadChars(32);
            mesh.TextureMap2 = mdlReader.ReadChars(32);

            //Debug.WriteLine("Tex1: "+new string(mesh.TextureMap1));
            //Debug.WriteLine("Tex2: " + new string(mesh.TextureMap2));

            mdlReader.BaseStream.Position += 24;
            mdlReader.BaseStream.Position += 12; // Vertex Indicies Counts

            AuroraFile.ReadArrayDef(mdlReader, out mesh.VertexLocArrayOffset, out mesh.VertexLocArrayCount);

            if (mesh.VertexLocArrayCount > 1)
                throw new Exception("Face offsets offsets count wrong "+ mesh.VertexLocArrayCount);

            mdlReader.BaseStream.Position += 12; // Unknown

            mdlReader.BaseStream.Position += (24 + 16); // Unknown

            mesh.MDXStructSize = mdlReader.ReadUInt32();

            mdlReader.BaseStream.Position += 8; // Unknown

            mesh.MDXVertexNormalsOffset = mdlReader.ReadUInt32();

            mdlReader.BaseStream.Position += 4; // Unknown

            //mesh.UV
            UInt32[] offUV = new UInt32[2];
            offUV[0] = mdlReader.ReadUInt32();
            offUV[1] = mdlReader.ReadUInt32();

            mdlReader.BaseStream.Position += 24; // Skip 24 Bytes

            mesh.VerticiesCount = mdlReader.ReadUInt16();
            mesh.TextureCount = mdlReader.ReadUInt16();

            mdlReader.BaseStream.Position += 2; // Skip 2 Bytes

            mesh.FlagShadow = mdlReader.ReadByte();
            bool _shadow = mesh.FlagShadow == 1;
            mesh.FlagRender = mdlReader.ReadByte();
            bool _render = mesh.FlagRender == 1;

            mdlReader.BaseStream.Position += 10; //Skip 10 Bytes

            if (Global.Game == Global.Games.KOTOR2) //Skipping these bytes will also let placeables work
                mdlReader.BaseStream.Position += 8; //Skip 8 Bytes

            mesh.MDXNodeDataOffset = mdlReader.ReadUInt32();
            mesh.VertexCoordinatesOffset = mdlReader.ReadUInt32();

            //Debug.WriteLine("MDX Offset: " + mesh.MDXNodeDataOffset + " MDX Length: " + mdxReader.BaseStream.Length);

            //Placeable & Some Items MDX Offset Fix
            if (mesh.MDXNodeDataOffset > mdxReader.BaseStream.Length)
            {
                //mdlReader.BaseStream.Position -= 2;
                //mesh.MDXNodeDataOffset = mdlReader.ReadUInt32();
            }

            //Head Items MDX Offset Fix
            if (mesh.MDXNodeDataOffset > mdxReader.BaseStream.Length)
            {
                mdlReader.BaseStream.Position -= 6;
                mesh.MDXNodeDataOffset = mdlReader.ReadUInt32();
            }

            //Debug.WriteLine("MDX Offset: " + mesh.MDXNodeDataOffset + " MDX Length: " + mdxReader.BaseStream.Length);

            if ((mesh.VertexLocArrayCount < 1) || (mesh.VerticiesCount == 0) || (mesh.FaceArrayCount == 0))
                return;

            long endPos = mdlReader.BaseStream.Position;

            if (mesh.TextureCount > 2)
            {
                //warning("Model_KotOR::readMesh(): textureCount > 2 (%d)", textureCount);
                mesh.TextureCount = 2;
            }

            String tMap1 = "";
            for(int i = 0; i!=mesh.TextureMap1.Length; i++)
            {
                if( ((int)mesh.TextureMap1[i]) != 0)
                {
                    tMap1 = tMap1 + mesh.TextureMap1[i];
                }
                else
                {
                    break;
                }
            }

            //  We need to load the texture from file.
            //textureImage = Paloma.TargaImage.LoadTargaImage("I_datapad.tga");
            if (tMap1 != "" && tMap1 != "NULL")
            {
                //Load the texture file here
            }


            //Debug.WriteLine("MDX Size: "+mdxReader.BaseStream.Length);
            mesh.tvectors = new List<vec2>();
            for (UInt32 i = 0; i < mesh.VerticiesCount; i++)
            {
                // Position
                mdxReader.BaseStream.Position = (mesh.MDXNodeDataOffset + (i * mesh.MDXStructSize));
                mesh.vectors.Add(new vec3(mdxReader.ReadSingle(), mdxReader.ReadSingle(), mdxReader.ReadSingle()));


                // Normal
                mesh.normals.Add(new vec3(mdxReader.ReadSingle(), mdxReader.ReadSingle(), mdxReader.ReadSingle()));
                
                // TexCoords
                for (UInt16 t = 0; t < 1; t++)
                {
                    if (offUV[t] != 0xFFFFFFFF)
                    {
                        mdxReader.BaseStream.Position = (mesh.MDXNodeDataOffset + i * mesh.MDXStructSize + offUV[t]);
                        mesh.tvectors.Add(new vec2(mdxReader.ReadSingle(), mdxReader.ReadSingle()));
                    }
                    else {
                        //mesh.tvectors.Add(new vec2(0.0f));
                    }
                }
            }

            mdlReader.BaseStream.Position = fileHeader.ModelDataOffset + mesh.VertexLocArrayOffset;
            UInt32 offVerts = mdlReader.ReadUInt32();

            mdlReader.BaseStream.Position = fileHeader.ModelDataOffset + offVerts;
            for (UInt32 i = 0; i < mesh.FaceArrayCount * 3; i++)
            {
                UInt16 index = mdlReader.ReadUInt16();
                mesh.faces.Add(mesh.vectors[index]);
                try {
                    mesh.texCords.Add(mesh.tvectors[index]);
                    mesh.uvs.Add(new vec2(mesh.tvectors[index].x, mesh.tvectors[index].y));
                }catch(Exception ex)
                {

                }
                //Debug.WriteLine(index);
            }
            meshes.Add(mesh);
        }

        public void MoveTo(float x, float y, float z)
        {
            position.x = x;
            position.y = y;
            position.z = z;
        }

        public void MoveTo(vec3 pos)
        {
            position = pos;
        }

        public struct FileHeader
        {
            public UInt32 FlagBinary;
            public UInt32 ModelDataSize;
            public UInt32 RawDataSize;
            public UInt32 ModelDataOffset;
            public UInt32 RawDataOffset;
        }

        public struct GeometryHeader
        {
            public long Unknown1;
            public long Unknown2;
            public char[] ModelName;
            public long RootNodeOffset;
            public long NodeCount;
            public byte[] Unknown3; //28Bytes Unknown
            public sbyte GeometryType;
            public byte[] Unknown4; //Padding? 3Bytes
        }

        public struct ModelHeader
        {
            public byte[] Unknown1;
            public sbyte Classification;
            public sbyte Fogged;
            public byte[] Unknown2;
            public UInt32 AnimationDataOffset;
            public UInt32 AnimationsCount;
            public UInt32 AnimationsAllocated;
            public UInt32 ParentModelPointer;
            public float BoundingMinX;
            public float BoundingMinY;
            public float BoundingMinZ;
            public float BoundingMaxX;
            public float BoundingMaxY;
            public float BoundingMaxZ;
            public float Radius;
            public float Scale;
            public char[] SuperModelName; //32Bytes NULL Terminated
        }

        public struct AnimationHeader {
            public long length;
            public long TransTime;
            public char[] ModelName; //32Chars NULL terminated
            public long EventsOffset;
            public long EventsCount;
            public long EventsAllocated; //Duplicate of Previous
            public byte[] _unknown; //Unkown 4Bytes
        }

        public struct AnimationEvent
        {
            public float ActivationTime; //float
            public char[] Name; //32Chars NULL terminated *CCHARGIN event
        }

        public enum ControllerTypes
        {
            Position = 8,
            Orientation = 20,
            Scale = 36,
            Color = 76,
            Radius = 88,
            ShadowRadius = 96,
            VerticalDisplacement = 100,
            Multiplier = 140,
            AlphaEnd = 80,
            AlphaStart = 84,
            BirthRate = 88,
            Bounce_Co = 92,
            ColorEnd = 96,
            ColorStart = 108,
            CombineTime = 120,
            Drag = 124,
            FPS = 128,
            FrameEnd = 132,
            FrameStart = 136,
            Grav = 140,
            LifeExp = 144,
            Mass = 148,
            P2P_Bezier2 = 152,
            P2P_Bezier3 = 156,
            ParticleRot = 160,
            RandVel = 164,
            SizeStart = 168,
            SizeEnd = 172,
            SizeStart_Y = 176,
            SizeEnd_Y = 180,
            Spread = 184,
            Threshold = 188,
            Velocity = 192,
            XSize = 196,
            YSize = 200,
            BlurLength = 204,
            LightningDelay = 208,
            LightningRadius = 212,
            LightningScale = 216,
            Detonate = 228,
            AlphaMid = 464,
            ColorMid = 468,
            PercentStart = 480,
            PercentMid = 481,
            PercentEnd = 482,
            SizeMid = 484,
            SizeMid_Y = 488,
            SelfIllumColor = 100,
            Alpha = 128
        }

        public struct Controller
        {
            public int Type;
            public UInt16 _unknown;
            public UInt16 ControllerDataRowCount;
            public UInt16 TimeKeyOffset;
            public UInt16 DataOffset;
            public char DataColumnCount;
            public byte[] _unknown2; //3Bytes
        }

        //80Bytes Total
        public class NodeHeader
        {
            public UInt16 NodeType; //2Byte Short
            public UInt16 Supernode; //2Byte Short
            public UInt16 NodePosition; //2Byte Short *CCHARGIN Node Number
            public string Name;
            public UInt16 Unknown; //2Byte Unknown
            public byte[] Unknown2; //4Byte Unknown
            public long ParentNodeLoc; //4Byte Long
            public float PositionX;
            public float PositionY;
            public float PositionZ;
            public float RotationW;
            public float RotationX;
            public float RotationY;
            public float RotationZ;

            //Child Nodes Array Pointers
            public UInt32 ChildNodesOffset; //4Byte Long
            public UInt32 ChildNodesCount; //4Byte Long
            public UInt32 ChildNodesAllocated; //4Byte Long //Duplicate of Previous

            //Node Controllers Array Pointers
            public UInt32 ControllerKeyOffset; //4Byte Long
            public UInt32 ControllerKeyCount; //4Byte Long
            public UInt32 ControllerKeyAllocated; //4Byte Long //Duplicate of Previous

            //Node Controllers Data Array Pointers
            public UInt32 ControllerDataOffset; //4Byte Long
            public UInt32 ControllerDataCount; //4Byte Long
            public UInt32 ControllerDataAllocated; //4Byte Long //Duplicate of Previous

            public vec3 position;

            public NodeHeader parent;

            internal void setParent(NodeHeader node)
            {
                this.parent = node;
            }
        }

        

        public class MeshNode
        {
            public long Unknown1;
            public long Unknown2;
            public UInt32 FaceArrayOffset;
            public UInt32 FaceArrayCount;
            public UInt32 FaceArrayAllocated;
            public float BoundingBoxMinX;
            public float BoundingBoxMinY;
            public float BoundingBoxMinZ;
            public float BoundingBoxMaxX;
            public float BoundingBoxMaxY;
            public float BoundingBoxMaxZ;
            public float Radius;
            public float PointsAverageX;
            public float PointsAverageY;
            public float PointsAverageZ;
            public float DiffuseR;
            public float DiffuseG;
            public float DiffuseB;
            public float AmbientR;
            public float AmbientG;
            public float AmbientB;
            public UInt32 TransparencyHint;
            public char[] TextureMap1;
            public char[] TextureMap2;
            public byte[] Unknown4;         //24Bytes
            public UInt32 VertexNumArrayOffset;
            public UInt32 VertexNumArrayCount;
            public UInt32 VertexNumArrayAllocated;
            public UInt32 VertexLocArrayOffset;
            public UInt32 VertexLocArrayCount;
            public UInt32 VertexLocArrayAllocated;
            public UInt32 UnknownArrayOffset;
            public UInt32 UnknownArrayCount;
            public UInt32 UnknownArrayAllocated;
            public long Unknown5;
            public long Unknown6;
            public long Unknown7;
            public long Unknown8;
            public long Unknown9;
            public long Unknown10;
            public byte[] Unkown11; //16Bytes
            public long MDXStructSize;
            public long Unknown12;//unknown (has something to do with textures)
            public long Unknown13; //Always 0 (NULL maybe?)
            public long MDXVertexNormalsOffset; //Always 12
            public long Unknown14; //Always -1
            public long MDXUVCoordinatesOffset;
            public byte[] Unkown15; //28Bytes - unknown (each value always -1)
            public UInt16 VerticiesCount;
            public UInt16 TextureCount;
            public short Unknown16;
            public short FlagShadow; //(value of 256 = cast shadow)
            public short FlagRender; //(value of 256 = render this node)
            public short Unknown17;
            public long Unknown18; //8Bytes
            public long MDXNodeDataOffset;
            public long VertexCoordinatesOffset;

            public List<vec3> vectors = new List<vec3>();
            public List<vec3> normals = new List<vec3>();
            public List<vec2> tvectors = new List<vec2>();
            public List<vec3> faces = new List<vec3>();
            public List<vec2> texCords = new List<vec2>();
            public List<vec2> uvs = new List<vec2>();

            //  Storage the texture itself.
            //public Bitmap textureImage;
            public NodeHeader parent;

            public void setParent(NodeHeader p)
            {
                parent = p;
            }

        }


    }
}
