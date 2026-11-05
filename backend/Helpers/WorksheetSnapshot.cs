using AumoBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace AumoBackend.Helpers;

/// <summary>
/// Snapshot kertas kerja (tabel Worksheet) menjadi sumber laporan keuangan. Perubahan data yang tidak
/// meninggalkan jejak waktu (hapus jurnal, ubah akun) harus membuang snapshot supaya laporan berikutnya
/// dihitung ulang.
/// </summary>
public static class WorksheetSnapshot
{
    /// <summary>Membuang seluruh snapshot milik user. Gagal membuang tidak boleh menggagalkan operasi pemanggil.</summary>
    public static async Task InvalidateAsync(AppDbContext db, Guid userId)
    {
        try
        {
            await db.Worksheet.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
        catch (Exception)
        {
            // Tabel Worksheet belum dibuat atau koneksi gagal: laporan akan menghitung ulang sendiri
            // saat snapshot tidak ditemukan, jadi aman untuk diabaikan di sini.
        }
    }
}
