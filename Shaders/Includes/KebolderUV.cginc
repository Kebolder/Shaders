#ifndef KEBOLDER_UV_INCLUDED
#define KEBOLDER_UV_INCLUDED

inline float2 KebolderUV(float2 uv, float4 st)
{
    return uv * st.xy + st.zw;
}

#endif
