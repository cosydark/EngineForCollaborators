#ifndef MM_EV_DEFAULTLIT_INCLUDED
#define MM_EV_DEFAULTLIT_INCLUDED

void LerpWhiteTo_float(float b, float t, out float Out)
{
    float oneMinusT = 1.0 - t;
    Out = oneMinusT + b * t;
}


#endif //MM_EV_DEFAULTLIT_INCLUDED