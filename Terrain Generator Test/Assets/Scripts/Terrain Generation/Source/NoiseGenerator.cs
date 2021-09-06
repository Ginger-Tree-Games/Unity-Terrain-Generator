using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator
{
    public float[,] MakeNoise(int noise_width, int noise_height, int seed, float frequency_x, float frequency_y, float amplitude, int octaves, float persistence, float lacunarity)
    {
        float[,] noise_map = new float[noise_width, noise_height];

        float max_noise = 0;
        float min_noise = 0;

        if (frequency_x <= 0)
        {
            frequency_x = 0.0001f;
        }

        if (frequency_y <= 0)
        {
            frequency_y = 0.0001f;
        }

        for (int z = 0; z < noise_height; z++)
        {
            for (int x = 0; x < noise_width; x++)
            {
                float y = 0f;

                float noise = 0f;

                for (int i = 0; i < octaves; i++)
                {

                    noise = Mathf.PerlinNoise(x * frequency_x * Mathf.Pow(lacunarity, i),
                        z * frequency_y * Mathf.Pow(lacunarity, i)) * 2 - 1;

                    y += noise * amplitude * Mathf.Pow(persistence, i);
                }

                if(y > max_noise)
                {
                    max_noise = y;
                }
                else if(y < min_noise)
                {
                    min_noise = y;
                }

                noise_map[x, z] = y;
            }
        }

        for (int z = 0; z < noise_height; z++)
        {
            for (int x = 0; x < noise_width; x++)
            {
                noise_map[z, x] = Mathf.InverseLerp(min_noise, max_noise, noise_map[x, z]);
            }
        }

        return noise_map;
    }
}
