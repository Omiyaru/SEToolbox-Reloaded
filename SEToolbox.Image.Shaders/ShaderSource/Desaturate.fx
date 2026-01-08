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
	float gColor;
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

		maxColor = srcColor.r;
		if (maxColor < srcColor.g)
			maxColor = srcColor.g;
		if (maxColor < srcColor.b)
			maxColor = srcColor.b;

		minColor = srcColor.r;
		if (minColor > srcColor.g)
			minColor = srcColor.g;
		if (minColor > srcColor.b)
			minColor = srcColor.b;

		
		float4 luminance = float4(0.2126f * srcColor.r + 0.7152f * srcColor.g + 0.0722f * srcColor.b, 0.2126f * srcColor.r + 0.7152f * srcColor.g + 0.0722f * srcColor.b, 0.2126f * srcColor.r + 0.7152f * srcColor.g + 0.0722f * srcColor.b, srcColor.a);
		
		// Convert RGB to Grayscale
		float3 rgb = float3(srcColor.rgb);
		float grayColor = 0.299f * rgb.r + 0.587f * rgb.g + 0.114f * rgb.b;
		float luminanceGray = luminance.r * 0.299f + luminance.g * 0.587f + luminance.b * 0.114f;
		float finalGray = luminanceGray < grayColor ? luminanceGray : grayColor;
		finalColor = float4(finalGray, finalGray, finalGray, srcColor.a);
	}
	return finalColor;
}

