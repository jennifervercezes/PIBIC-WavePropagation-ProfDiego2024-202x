using UnityEngine;
using System;
using System.IO;

public class WaveBehavior : MonoBehaviour
{
    public const float PI = 3.1415926f;
    public const int NORMAL = 1;

    // ---- VARIABLES
    float[,] fip, fic, fif; // Past, Current and Future matrices

    /* 
    The parameters are only instantiated in memory at Start() and not in the class constructor
    because it allows for more control over the initialization process and avoids unnecessary 
    memory allocation.This is important for performance, especially in Unity where the Start() 
    method is called only when the MonoBehavior is instanciated by the program.
    */
    float t, t0, tfim,         // time step, start -> current -> end
        x, x0, y, y0,          // x & y steps, start -> current
        psix, psiy,            // x & y source position
        xfim, yfim,            // x & y position future (end)
        xpulse, ypulse;        // x & y position pulse/step

    float delta, deltat, deltax, deltay, // time, xpos, ypos infinitesimal variances
        dtdx, dtdy,                      // derivatives of time in respect to x and y
        csx, csy, cdx, cdy,              // method velocity in respect to x and y
        tau, l;                          // tau = time step, l = length of the wave

    int i, j, k;               // i, j, k are indexes for the matrix
    int ip, jp;                // i and j are the index of current x, y position in the matrix
    int fieldtype;
    long n, nx, ny, nt;    // n and the n-th x, y position and t moment

    // ---- FUNCTONS
    float c (float x, float y, int fieldtype = 1)
    {
        float cc = 0.9f;

        if(fieldtype == 0)       // Field Type 0 Senoidal
            if (x >= 2.0f && y >= 2.0f) 
                cc = 0.6f;
        else if (fieldtype == 1) // Field Type 1 Stratificated
        { 
            if (y >= 2.0f && y <= 3.0f)
                cc = 1.4f;
            else if (y <= 4.0f) 
                cc = 1.2f;
            else          
                cc = 0.8f;
        }
        else if (fieldtype == 2) // Field Type 2 Gaussian
        {
            if (y >= 2.0f && y <= 3.0f && x >= 2.0f && x <= 3.0f)
                cc = 1.6f;
            else
                cc = 0.8f;
        }
        else if (fieldtype == 3) // Field Type 3 Uniform
            return cc;
        return cc;
    }
    // Velocity Field / Soil, Fluid, etc.
    float f (float x, float y)
    {
        if (x >= 0.0f && x <= PI/4.0f)                             //Field Type 0 
                return(Mathf.Sin(PI * x));
        else if (x >= 0.0f && y >= 0.0f && x <= 0.3f && y <= 0.3f) //Field Type 1
                return(1.0f);
        else                                                       //Field Type 2
            return(0.0f);
    } 
    // Initial Condition for Amplitude 
    float g(float x, float y){return(0.0f);}
    // Initial Condition for Amplitude Expansion Velocity
    float psi(float t, float x0, float y0, float x, float y, float delta, int fieldtype = 1)
    {
        float a = 8.0f; // Amplitude of the Gaussian

        // 2nd Gaussian Derivative. Similar to Andre Bulcao's Article.
        if(fieldtype != 0)
        {
            /* Gaussian Source */
            float b = a * fieldtype * (t - 0.2f);
            
            if ((x >= x0 - delta) && (x <= x0 + delta) && (y >= y0 - delta) && (y <= y0 + delta))
                return(10000.0f * (1 - b * b) * Mathf.Exp(- b * b));
            else
                return(0.0f);
        }
        else
        {
            /* Senoidal Source */
            float omega = 10.0f; a = 10000.0f;
            
            if ((x >= 2.5f) && (x <= 2.55f) && (y >= 2.5f) && (y <= 2.55f))
                return(a * Mathf.Sin(omega * t));       
            else          
                return(0.0f);
        }
    }
    // Font/Source 

    void Initialize() 
    {
        //Parameters for the Discrete Solution fot the Differential Equation Problem
        //fieldtype = NORMAL; // Field Type 0, 1, 2, 3, etc.
        // Field Type 0 Senoidal, 1 Stratificated, 2 Gaussian, 3 Uniform, etc.
        
        // Variance/Step from t1 to t2, x1 to x2, y1 to y2
        deltat = 0.001f;  // Discretization of time 
        deltax = 0.02f;   // Discretization of space in x 
        deltay = 0.02f;   // Discretization of space in y 

        // Discribing Time of Simulation
        t0   = 0.0f;
        tfim = 5.5f;

        // Discribing Space
        x0   = 0.0f; y0   = 0.0f; 
        xfim = 5.0f; yfim = 5.0f;

        // Source Position
        psix = 1.0f; psiy = 1.0f;
        // Tolerance for the Source Position
        delta = deltax;

        // Gets an n number of points in the space/time mesh to generate a set of discrete data
        nt = (long) ((tfim - t0)/deltat);
        nx = (long) ((xfim - x0)/deltax) + 1;
        ny = (long) ((yfim - y0)/deltay) + 1;

        // Alocates space for the 2D matrices 
        /* 
        float[,] is more efficient than float[][] in C# for 2D arrays
        because it is a contiguous block of memory, while float[][] is 
        an array of arrays. Do I need a throw exception for NULL here? 
        I think not, because the matrix is already initialized with 0.0f
        */
        
        fip = new float[nx, ny]; // Past Matrix
        fic = new float[nx, ny]; // Current Matrix
        fif = new float[nx, ny]; // Future Matrix

        Debug.Log($"Number of intervals, time {nt} & spacial {nx}, {ny}");

        // Auxiliary variables for the method
        dtdx = deltat/deltax;       // Derivative of time in respect to x
        dtdy = deltat/deltay;       // Derivative of time in respect to y
        xpulse = (xfim - x0)/2.0f;   // x position of the pulse/step
        ypulse = (yfim - y0)/2.0f;   // y position of the pulse/step
        
        // Initial Conditions for Amplitude
        for (i = 0; i < nx; i++)
        {
            for (j = 0; j < ny; j++)
            {
                x = x0 + i * deltax;
                y = y0 + j * deltay;
                fip[i, j] = f(x - xpulse, y - ypulse);
            }
        }
    }

    void FirstStep()
    {
        t = 0f;

        for (i = 1; i < nx - 1; i++)
        {
            x = x0 + i * deltax;
            for (j = 1; j < ny - 1; j++)
            {
                y = y0 + j * deltay;
                cdx = dtdx * c(x, y, 1);
                csx = cdx  * cdx;
                cdy = dtdy * c(x, y, 1);
                csy = cdy  * cdy;

                fic[i, j] = (1 - csx - csy) * fip[i, j]
                          + 0.5f * csx * (fip[i + 1, j] + fip[i - 1, j])
                          + 0.5f * csy * (fip[i, j + 1] + fip[i, j - 1])
                          + deltat * g(x, y)
                          + 0.5f * deltat * deltat * psi(t, psix, psiy, x, y, delta);
            }
        }
    }
    void NthStep()
    {
        t = 0f;

        for (n = 1; n < nt; n++)
        {
            t += deltat;

            for (i = 1; i < nx - 1; i++)
            {
                x = x0 + i * deltax;

                for (j = 1; j < ny - 1; j++)
                {
                    y = y0 + j * deltay;
                    cdx = dtdx * c(x, y);
                    csx = cdx  * cdx;
                    cdy = dtdy * c(x, y);
                    csy = cdy  * cdy;

                    fif[i, j] = 2.0f * (1 - csx - csy) * fic[i, j]
                              + csx * (fic[i + 1, j] + fic[i - 1, j])
                              + csy * (fic[i, j + 1] + fic[i, j - 1])
                              - fip[i, j]
                              + deltat * deltat * psi(t, psix, psiy, x, y, delta);

                    // Absorbing Boundary Conditions
                    fif[0, j] = fic[0, j] + fic[1, j] - fip[1, j]
                              + cdx * (fic[1, j] - fic[0, j] - fip[2, j] + fip[1, j]);

                    fif[nx - 1, j] = fic[nx - 1, j] + fic[nx - 2, j] - fip[nx - 2, j]
                                   - cdx * (fic[nx - 1, j] - fic[nx - 2, j] - fip[nx - 2, j] + fip[nx - 3, j]);
                }

                fif[i, ny - 1] = fic[i, ny - 1] + fic[i, ny - 2] - fip[i, ny - 2]
                               - dtdy * (fic[i, ny - 1] - fic[i, ny - 2] - fip[i, ny - 2] + fip[i, ny - 3]);
            }

            float[,] temp = fip;
            fip = fic;
            fic = fif;
            fif = temp;
            // Swap matrices for the next iteration
            // This is a more efficient way to swap matrices in C# than using pointers
            /*
            int transfer(float **a, float **b, int n, int m); ---- Old Code

            Transfer Function that swaps a fip (force in past) to fic (force in current) 
            then to a fif (force in future), both described by pointers of pointers, 
            will be resignificated to 2D matrices that are swaped in real time (Update) 
            only using up the alocation of a tempforce, for optimization purposes.
            */
        }
    }

    void SaveBin(string path)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            for (i = 0; i < nx; i++)
                for (j = 0; j < ny; j++)
                    writer.Write(fif[i, j]);
        }
    }

    void SaveText(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            for (i = 0; i < nx; i++)
            {
                for (j = 0; j < ny; j++)
                {
                    writer.Write(fif[i, j].ToString("F4") + " ");
                }
                writer.WriteLine();
            }
        }
    }


    private void Start() 
    {
        Initialize(); // Initialize the Parameters & Matrices
        FirstStep();  // Initialize Conditions for Amplitude Expansion Velocity
    }

    private void Update() 
    {
        NthStep();    // Iterates to N-th Step of the Method
        
        //f(x, y) => 0f;
        //g(x, y) => 0f;

        // Save Wave Positions File as Binary and Text
        // The path is set to the StreamingAssets folder, which is accessible at runtime
        SaveBin(Application.streamingAssetsPath + "/Wave.bin");
        SaveText(Application.streamingAssetsPath + "/Wave.dat");
    }
}