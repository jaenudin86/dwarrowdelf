﻿
struct VS_IN
{
	float3 pos : POSITION;
	int texID : TEXID;
};

struct GS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	int texID : TEXID;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 posW : POSITION;
	float2 tex : TEXCOORD0;
	nointerpolation int texID : TEXID;
};

Texture2DArray blockTextures;
sampler blockSampler;

cbuffer PerFrame
{
	matrix g_viewProjMatrix;

	float4 ambientColor;
	float4 diffuseColor;
	float4 specularColor;
	float3 lightDirection;
	float _pad0;
	float3 g_eyePos;
	float _pad1;
	bool g_showBorders;
	float _pad2;
	float _pad3;
	float _pad4;
};

cbuffer PerObjectBuffer
{
	matrix worldMatrix;
};

GS_IN VSMain(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	// Change the position vector to be 4 units for proper matrix calculations.
	float4 pos = float4(input.pos, 1.0f);

	output.pos = mul(pos, worldMatrix);
	output.posW = output.pos.xyz;
	output.pos = mul(output.pos, g_viewProjMatrix);

	output.texID = input.texID;

	return output;
}

[maxvertexcount(6)]
void GSMain(lineadj GS_IN input[4], inout TriangleStream<PS_IN> OutputStream)
{
	PS_IN output = (PS_IN)0;

	float texID = input[0].texID;

	/* FIRST */
	output.pos = input[0].pos;
	output.posW = input[0].posW;
	output.tex = float2(0, 0);

	output.texID = texID;

	OutputStream.Append(output);

	output.pos = input[1].pos;
	output.posW = input[1].posW;
	output.tex = float2(1, 0);
	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.posW = input[2].posW;
	output.tex = float2(0, 1);
	OutputStream.Append(output);

	/* SECOND */
	output.pos = input[1].pos;
	output.posW = input[1].posW;
	output.tex = float2(1, 0);
	OutputStream.Append(output);

	output.pos = input[2].pos;
	output.posW = input[2].posW;
	output.tex = float2(0, 1);
	OutputStream.Append(output);

	output.pos = input[3].pos;
	output.posW = input[3].posW;
	output.tex = float2(1, 1);

	output.texID = texID;

	OutputStream.Append(output);

	OutputStream.RestartStrip();
}

float4 PSMain(PS_IN input) : SV_Target
{
	float d = 0.01f;

	float3 toEye = normalize(g_eyePos - input.posW);

	// Invert the light direction for calculations.
	float3 lightDir = -lightDirection;

	float4 ambient, diffuse, specular;

	ambient = ambientColor;

	float3 normal = cross(ddy(input.posW.xyz), ddx(input.posW.xyz));
	normal = -normalize(normal);

	float lightIntensity = dot(normal, lightDir);

	diffuse = specular = 0;

	if (lightIntensity > 0.0f)
	{
		diffuse = lightIntensity * diffuseColor;

		float3 v = reflect(-lightDir, normal);
			float specFactor = pow(max(dot(v, toEye), 0.0f), 64);
		specular = specFactor * specularColor;
	}

	float4 litColor = ambient + diffuse + specular;

	float4 textureColor = blockTextures.Sample(blockSampler, float3(input.tex, input.texID));

	float4 color = litColor * textureColor;

	if (g_showBorders)
	{
		float val = min(input.tex.x, input.tex.y);
		val = min(val, (1.0f - input.tex.x));
		val = min(val, (1.0f - input.tex.y));

		val = smoothstep(0, 0.1f, val) * 0.3f + 0.7f;

		color *= val;
	}

	return color;
}

technique
{
	pass
	{
		Profile = 10.0;
		VertexShader = VSMain;
		GeometryShader = GSMain;
		PixelShader = PSMain;
	}
}
