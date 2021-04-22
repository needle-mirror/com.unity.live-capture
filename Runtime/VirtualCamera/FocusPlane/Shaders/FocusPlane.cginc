float _CameraDepthThreshold;
float4 _Color;
int _GridResolution;
float _GridLineWidth;
float _IntersectionLineWidth;
float _BackgroundOpacity;

float4 FocusPlane(float eyeSpaceDepth, float2 positionNDC, float screenAspect)
{
    // Relative camera-space depth to the focus distance.
    float deltaDepth = eyeSpaceDepth - _CameraDepthThreshold;
    
    // We are in front of the focus plane, nothing to render.
    if (deltaDepth < _IntersectionLineWidth * -0.5)
        return float4(0, 0, 0, 0); 
       
    // Evaluate the grid.
    float2 steps = frac(positionNDC * _GridResolution * float2(screenAspect, 1));
    // Smooth grid outlines.
    float2 grid2d = pow(max(
        smoothstep(_GridLineWidth * 0.5, 0, steps), 
        smoothstep(1 - _GridLineWidth * 0.5, 1, steps)), 4);
  
    // At this point we have discarded fragments who are in front of the focus plane by a margin corresponding to (_IntersectionLineWidth * -0.5).
    // We need the grid to be *precisely* at the focus distance, hence the use of (beyondDepthThreshold).
    float beyondDepthThreshold = deltaDepth > 0;
  
    float grid = (grid2d.x + grid2d.y) * beyondDepthThreshold;
    
    // Evaluate the intersection with scene geometry, which is characterized by a very small deltaDepth,
    // aka "this fragment's depth is approximately the focus distance".
    float edge = pow(lerp(
        smoothstep(-0.5 * _IntersectionLineWidth, 0, deltaDepth), 
        smoothstep(0.5 * _IntersectionLineWidth, 0, deltaDepth), beyondDepthThreshold), 4);
    
    // Combine the grid, intersection and background opacity to determine the final opacity.
    float opacity = grid + edge + _BackgroundOpacity * beyondDepthThreshold;
        
    return float4(_Color.rgb, _Color.a * opacity);
}
