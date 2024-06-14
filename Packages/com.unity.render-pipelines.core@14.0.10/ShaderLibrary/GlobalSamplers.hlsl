#ifndef UNITY_CORE_SAMPLERS_INCLUDED
#define UNITY_CORE_SAMPLERS_INCLUDED

// Common inline samplers.
// Separated into its own file for robust including from any other file.
// Helps with sharing samplers between intermediate and/or procedural textures (D3D11 has a active sampler limit of 16).
SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;  
SamplerState sampler_PointRepeat;
SamplerState sampler_LinearRepeat;
SamplerState sampler_TriLinearClamp;
SamplerState sampler_TriLinearRepeat;
SamplerState sampler_LinearRepeatAniso8;
SamplerState sampler_LinearClampAniso8;

#endif //UNITY_CORE_SAMPLERS_INCLUDED
