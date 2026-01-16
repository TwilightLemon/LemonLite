// compile: fxc /T ps_3_0 /E main /Fo TextGlow.ps TextGlow.hlsl
sampler2D input : register(s0);

float pos : register(c0);
float width : register(c1);
float4 highlighter : register(c2);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(input, uv);
    float d = max(0, uv.x - pos);
    float glow = saturate(1 - d / width);
    glow = glow * glow;
    float mask = step(0.0, color.a);
    color.rgb += highlighter.rgb * glow * mask * highlighter.a;
    return color;
}