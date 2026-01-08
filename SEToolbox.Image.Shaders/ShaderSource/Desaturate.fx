//--------------------------------------------------------------------------------------
//
// WPF ShaderEffect HLSL -- DesaturateEffect
//
//--------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------
// Sampler Inputs (Brushes, including ImplicitInput)
//--------------------------------------------------------------------------------------

sampler2D  implicitInputSampler : register(S0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 Main(float2 uv : TEXCOORD) : COLOR
{
	float2 texuv = uv;
	float4 finalColor;
	float maxColor;
	float minColor;
	float grayColor;

	float4 srcColor = tex2D(implicitInputSampler, texuv);

	if( srcColor.a == 0 )
	{
		finalColor = srcColor;
	}
	else
	{
		// Desaturate algorithm.  Replicates the Photoshop Desaturate filter.
		// should acount for the Alfa now
		float maxRgb = max(max(srcColor.r, srcColor.g), srcColor.b);
		float minRGB = min(min(srcColor.r, srcColor.g), srcColor.b);

		float luminance = dot(float3(0.2126f, 0.7152f, 0.0722f), srcColor.rgb);
		float luminanceGray = luminance * srcColor.a;
		float finalGray = luminanceGray < maxRgb ? luminanceGray : maxRgb;
		finalColor = float4(finalGray, finalGray, finalGray, srcColor.a);
	}
	return finalColor;
}

