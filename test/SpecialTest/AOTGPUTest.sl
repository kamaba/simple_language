# ============================================================
# AOTGPUTest - @GPU() 标注的 AOT 方法 MLIR GPU 编译测试
# 大张量矩阵乘（C[M,N] = A[M,K] * B[K,N]，矩阵用一维数组存储）
#
# 三个实现对比：
#   GpuMatMul : @AOT + @GPU → MLIR gpu.module/gpu.func kernel，
#               host 侧 gpu.launch_func 调度。
#               外层行循环 i 自动并行化为 grid-stride：
#                 i  = blockIdx.x * blockDim.x + threadIdx.x
#                 i += gridDim.x * blockDim.x
#               内层 k/j 循环每线程顺序执行。
#   CpuMatMul : 同逻辑 @AOT（CPU 原生执行对照）
#   VmMatMul  : 同逻辑无 @AOT（CVM 解释执行对照）
# ============================================================

class AOTGPUTest
{
    # GPU kernel：@GPU( tile 宽, tile 高, tileNum, groupId,
    #                   gridX, gridY, gridZ, blockX, blockY, blockZ,
    #                   sharedMem, deviceId, kernelName )
    # gridX=0 表示按循环上界自动推导块数；blockX=256 为默认线程块大小
    @AOT()
    @GPU( 0, 0, 0, 0, 0, 0, 0, 256, 1, 1, 0, 0, "" )
    static void GpuMatMul( double[] a, double[] b, double[] c, int M, int N, int K )
    {
        int i = 0
        for i = 0, i < M, i += 1
        {
            int k = 0
            for k = 0, k < N, k += 1
            {
                double s = 0.0
                int j = 0
                for j = 0, j < K, j += 1
                {
                    s = s + a[i * K + j] * b[j * N + k]
                }
                c[i * N + k] = s
            }
        }
    }

    # CPU AOT 对照：同逻辑原生执行（数组走 VMArray length/data 解引用）
    @AOT()
    static void CpuMatMul( double[] a, double[] b, double[] c, int M, int N, int K )
    {
        int i = 0
        for i = 0, i < M, i += 1
        {
            int k = 0
            for k = 0, k < N, k += 1
            {
                double s = 0.0
                int j = 0
                for j = 0, j < K, j += 1
                {
                    s = s + a[i * K + j] * b[j * N + k]
                }
                c[i * N + k] = s
            }
        }
    }

    # 解释器对照：同逻辑 CVM 解释执行（无 @AOT 标注）
    static void VmMatMul( double[] a, double[] b, double[] c, int M, int N, int K )
    {
        int i = 0
        for i = 0, i < M, i += 1
        {
            int k = 0
            for k = 0, k < N, k += 1
            {
                double s = 0.0
                int j = 0
                for j = 0, j < K, j += 1
                {
                    s = s + a[i * K + j] * b[j * N + k]
                }
                c[i * N + k] = s
            }
        }
    }
}
