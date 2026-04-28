Shader "AY_Shader/Skybox/StarrySkyLight"
{
    Properties
    {
        [Header(BaseSetting)]
        _BaseColor ("BaseColor[空のベースカラー]", Color) = (0.1,0.1,0.2,1)
        _GroundColor ("GroundColor[地平線のカラー]", Color) = (0.6,0.6,1.0,1)
        _GroundRef ("GroundRef[地平線の明るさ]", Range(0.0,10.0)) = 0.2
        _RefDamp ("RefDamp[GroundRefの減衰]", Range(1.0,30.0)) = 10.0
        _GroundOffset ("GroundOffset[地平線のオフセット]", Range(-1.0,1.0)) = 0.0
        _RotateSpeed("RotateSpeed[天の回転速度]",Range(0.0,1.0)) = 0.0
        _RotateAxis("RotateAxis[天の回転軸]",Vector) = (1,0.4,0.9,0)
        
        _Mask ("Mask[天の川のマスク]", 2D) = "white" {}
        _StarMaskRate ("StarMaskRate[星へのMask適用率]", Range(0.0,1.0)) = 0.25

        [Header(StarSetting)]
        _ProjectionDist ("ProjectionDist[星の投影距離]", Float) = 10.0
        _StarRange ("StarRange[星の光る範囲]", Range(0.01, 0.5)) = 0.3
        _StarBrightness ("StarBrightness[星の輝度]", Range(0.0, 5.0)) = 2.0
        _SecondRate ("SecondRate[二等星のサイズ倍率]", Range(1.1,10.0)) = 2.0
        _ThirdRate ("ThirdRate[三等星のサイズ倍率]", Range(1.2,10.0)) = 4.0
        [HDR]_StarColorBase("StarColorBase[星のベースカラー]", Color) = (0.3,0.4,1.0,1)
        _StarColorFluct("StarColorFluct[星のランダムカラーの強さ]", Range(0.0, 1.0)) = 0.3
        _ReduceBlink("ReduceBlink[低解像度時の明滅抑制]", Range(0.0,10.0)) = 0.0

        [Header(CloudSetting)]
        _CloudScale("CloudScale[星雲ノイズのスケール]", Range(0.01, 20.0)) = 3.0
        _NoiseTex("NoiseTex[星雲ノイズの3Dtex]", 3D) = "white" {}
        //[IntRange]_Octave ("Octave[星雲ノイズのディテール]", Range(1, 8)) = 4
        [HDR]_CloudColorBase("CloudColorBase[星雲のベースカラー]", Color) = (0.2,0.2,1.0,1)
        _CloudColorFluct("CloudColorFluct[星雲のランダムカラーの強さ]", Range(0.0, 1.0)) = 0.3

        [Header(ShootingStarSetting)]
        [Toggle] _IsShoot ("IsShoot[流星のONOFF]", Float) = 1
        [HDR]_ShootColorBase("ShootColorBase[流星のベースカラー]", Color) = (1.0,1.0,0.9,1)
        _ShootSpeed("ShootSpeed[流星の再生速度]", Range(0.0, 3.0)) = 0.7
        _ShootBrightness("ShootBrightness[流星の輝度]", Range(0.0, 5.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="SkyBox" }

        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _ _ISSHOOT_ON

            #include "UnityCG.cginc"

            sampler2D _Mask;
            sampler3D _NoiseTex;
            fixed4 _BaseColor, _GroundColor, _StarColorBase, _CloudColorBase,_ShootColorBase;
            float4 _RotateAxis;
            float _GroundRef, _RefDamp, _GroundOffset, _RotateSpeed, _StarMaskRate,
            _ProjectionDist, _StarRange, _StarBrightness, _SecondRate, _ThirdRate, _StarColorFluct, _ReduceBlink,
            _CloudScale, _CloudColorFluct, _ShootSpeed, _ShootBrightness;
            int _Octave;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0; //float3正規化されていないカメラ方向
            };

            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float3 random3d (float3 st){
                //0-1のfloat3
                st = float3(dot(st, float3(127.1, 311.7, 167.3)), dot(st, float3(269.5, 183.3, 32.1)), dot(st, float3(123.4, 321.9, 74.5)));
                return frac(sin(st) * 43758.5453123);
            }

            fixed4 drawStar (float3 i, float distRate, float mask){

                fixed4 color = 0;
                float3 dir = normalize(i);
                float3 pos = dir * _ProjectionDist * distRate;//視線方向に移動した地点の座標
                float3 ist = floor(pos);
                float3 fst = frac(pos);

                float pxSize = saturate(length(ddx(pos))*_ReduceBlink);
                float powCount = lerp(20,0.5,pxSize);

                //周囲の2*2*2セルを探索
                for (int x = 0; x <= 1; x++)
                for (int y = 0; y <= 1; y++)
                for (int z = 0; z <= 1; z++)
                {
                    //自身と0.5ずつ重なる2*2*2セルの座標
                    float3 neighbor = float3(x-0.5, y-0.5, z-0.5);
                    
                    //星のセル内ローカル座標
                    float3 p = random3d(ist + neighbor);

                    //星と描画ピクセルとの距離
                    float diff = length(neighbor + p - fst);
                    
                    //距離が0.5以上だった場合、次のループへ
                    if (diff >= 0.5) continue;

                    //星を球面まで移動させて再計算
                    float3 pdir = normalize(p+ist+neighbor);
                    float3 gp = pdir * _ProjectionDist * distRate;
                    p = gp - ist - neighbor;
                    diff = length(neighbor + p - fst);

                    //星の色
                    float4 randColor = float4(abs(p)*_StarColorFluct+(1-_StarColorFluct)*_StarColorBase, 1);
                    
                    //光の強さ p.xを利用して個体差 最大0.5
                    float starRate = saturate(_StarRange * 0.6 +  p.x * 0.2) * mask;
                    
                    //powの回数を距離に応じて減らす
                    float intensity = pow(saturate(starRate -length(diff))/starRate + starRate * 0.1,powCount) * saturate(1.0 - pxSize);

                    color += saturate(randColor*intensity);

                }
                return color;
            }

            fixed get3Dtex (float3 st){
                return tex3D(_NoiseTex, st);
            }

            float random (float2 p) { 
                return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
            }

            float dist(float2 p,float r1, float h)
            {
                float r2 = r1 * 0.1;
                float2 q = float2( length(p.x), p.y );
    
                float b = (r1-r2)/h;
                float a = sqrt(1.0-b*b);
                float k = dot(q,float2(-b,a));
    
                if( k < 0.0 ) return length(q) - r1;
                if( k > a*h ) return length(q-float2(0.0,h)) - r2;
        
                float col = dot(q, float2(a,b) ) - r1;
                return col;
            }

            fixed4 shootingStar (float3 st){
                float3 dir = normalize(st);
                float2 rad = float2(atan2(dir.x, dir.z),asin(dir.y));
                float2 uv = rad / float2(2.0 * UNITY_PI, UNITY_PI / 2);

                float timeBase = _Time.y * _ShootSpeed;
                float leng = 0.8;
                float timef = frac(timeBase)*leng;
                float timei = floor(timeBase);
                float timeCos = cos(timeBase*UNITY_PI*2)*0.5+0.5;
                float timeSin = sin(timeBase*UNITY_PI*2)*0.5+0.5;
                float r = 0.003 * (1-timeCos);

                fixed4 col = pow(saturate(-dist(uv+float2(random(timei)-0.5,timef)//y軸方向下に落とすため
                //fixed4 col = pow(saturate(-dist(uv+float2(0,timef)//確認用
                -float2(0,leng),r,timef)*300.0),5);

                return col*_ShootColorBase*_ShootBrightness;
            }

            float3 rotate(float3 p, float angle, float3 axis){
                float3 a = normalize(axis);
                float s = sin(angle);
                float c = cos(angle);
                float r = 1.0 - c;
                float3x3 m = float3x3(
                    a.x * a.x * r + c,
                    a.y * a.x * r + a.z * s,
                    a.z * a.x * r - a.y * s,
                    a.x * a.y * r - a.z * s,
                    a.y * a.y * r + c,
                    a.z * a.y * r + a.x * s,
                    a.x * a.z * r + a.y * s,
                    a.y * a.z * r - a.x * s,
                    a.z * a.z * r + c
                );
                return mul(p, m);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = _BaseColor;

                //天球の回転
                float3 RotateTex = rotate(i.texcoord,_Time.y*_RotateSpeed,_RotateAxis.xyz);
                
                //マスク
                fixed4 mask = tex2D(_Mask,RotateTex.xz*0.5+0.5);
                fixed starMask = mask.r * _StarMaskRate + (1-_StarMaskRate);
                
                //星
                color += drawStar(RotateTex, 1, starMask)*_StarBrightness;
                color += drawStar(RotateTex, _SecondRate, starMask)*_StarBrightness;
                color += drawStar(RotateTex, _ThirdRate, starMask)*_StarBrightness;

                //地平線
                color += pow(saturate(cos((i.texcoord.y+_GroundOffset) * UNITY_PI)),_RefDamp)*_GroundRef * _GroundColor;

                //星雲
                float3 dir = normalize(RotateTex);
                float3 pos = dir * 10.0/_CloudScale;
                //fixed4 randColor = fixed4(simplexNoise(pos*0.5+1),simplexNoise(pos*0.5+2),simplexNoise(pos*0.5+3),1)
                fixed4 randColor = fixed4(get3Dtex(pos*0.5+0.3),get3Dtex(pos*0.5+0.6),get3Dtex(pos*0.5+0.9),1)
                * _CloudColorFluct + _CloudColorBase * (1 - _CloudColorFluct);
                //fixed4 starCloud = fBm(pos) * randColor * mask;
                fixed4 starCloud = get3Dtex(pos) * randColor * mask;
                color += starCloud;

                //流星
                #ifdef _ISSHOOT_ON
                    color += shootingStar(i.texcoord);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
