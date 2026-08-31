using Microsoft.EntityFrameworkCore;
using Squadra.Server.Context;
using Squadra.Server.Exceptions;
using Squadra.Server.Modules.Wiadomosci.DTO;
using Squadra.Server.Modules.Wiadomosci.Enums;

namespace Squadra.Server.Modules.Wiadomosci.Repositories;

public class WiadomosciRepository(AppDbContext context) : IWiadomosciRepository
{
    public static readonly int MaksymalnaLiczbaWiadomosciNaCzaciePrywatnym = 300;
    public static readonly int MaksymalnaLiczbaWiadomosciNaCzacieDruzynowym = 250;
    
    public async Task<WiadomoscDto> GetWiadomosc(int id)
    {
        var wiadomosc = await context.Wiadomosc.FindAsync(id);
        if(wiadomosc == null) throw new NieZnalezionoWBazieException("Wiadomosc o id " + id + " nie istnieje");
        return new WiadomoscDto(
            wiadomosc.IdNadawcy,
            wiadomosc.IdOdbiorcy,
            wiadomosc.DataWyslania.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            wiadomosc.Tresc,
            wiadomosc.IdTypuWiadomosci
        );
    }

    // pobieramy wiadomości na czacie prywatnym między dwom użytkownikami
    public async Task<ICollection<WiadomoscDto>> GetWiadomosciPrywatne(int idUzytkownika1, int idUzytkownika2)
    {
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna &&
                                   ((x.IdNadawcy == idUzytkownika1 && x.IdOdbiorcy == idUzytkownika2) ||
                                  (x.IdNadawcy == idUzytkownika2 && x.IdOdbiorcy == idUzytkownika1))
            )
            .ToListAsync();
        wiadomosci.Sort((x,y) => x.DataWyslania.CompareTo(y.DataWyslania));
        return wiadomosci.Select(wiadomosc => new WiadomoscDto(
            wiadomosc.IdNadawcy,
            wiadomosc.IdOdbiorcy,
            wiadomosc.DataWyslania.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            wiadomosc.Tresc,
            wiadomosc.IdTypuWiadomosci
        )).ToList();
    }

    public async Task<ICollection<WiadomoscDto>> GetWiadomosciNaCzacieDruzyny(int idDruzyny)
    {
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Druzynowa && x.IdOdbiorcy == idDruzyny)
            .ToListAsync();
        wiadomosci.Sort((x,y) => x.DataWyslania.CompareTo(y.DataWyslania));
        return wiadomosci.Select(wiadomosc => new WiadomoscDto(
            wiadomosc.IdNadawcy,
            wiadomosc.IdOdbiorcy,
            wiadomosc.DataWyslania.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            wiadomosc.Tresc,
            wiadomosc.IdTypuWiadomosci
        )).ToList();
    }
    
    // nie obchodzi nas, kto jest nadawcą, a kto odbiorcą, więc bierzemy max z obu
    public async Task<DateTime?> GetDataNajnowszejWiadomosciPrywatnej(int idUzytkownika1, int idUzytkownika2) {
        var wiadomosci = await context.Wiadomosc
            .Where(x => (x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna &&
                                   x.IdNadawcy == idUzytkownika1 && x.IdOdbiorcy == idUzytkownika2) ||
                                  (x.IdNadawcy == idUzytkownika2 && x.IdOdbiorcy == idUzytkownika1))
            .ToListAsync();
        if(wiadomosci.Count == 0) return null;
        return wiadomosci.Max(x => x.DataWyslania);
    }
    
    public async Task<DateTime?> GetDataNajnowszejWiadomosciWDruzynie(int idDruzyny) {
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Druzynowa && x.IdOdbiorcy == idDruzyny)
            .ToListAsync();
        if(wiadomosci.Count == 0) return null;
        return wiadomosci.Max(x => x.DataWyslania);
    }
    
    public async Task<bool> CreateWiadomosc(int idOdbiorcy, WiadomoscCreateDto wiadomosc, int idNadawcy)
    {
        var wiadomoscDoDodania = new Models.Wiadomosc
        {
            IdNadawcy = idNadawcy,
            IdOdbiorcy = idOdbiorcy,
            DataWyslania = DateTime.UtcNow, // liczymy moment, w którym dotrze do bazy
            Tresc = wiadomosc.Tresc,
            IdTypuWiadomosci = wiadomosc.IdTypuWiadomosci
        };
        await context.Wiadomosc.AddAsync(wiadomoscDoDodania);
        if(wiadomosc.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna) await UsunWiadomosciPrywatnePrzekraczajaceLimit(idNadawcy, idOdbiorcy); // usuwamy nadmiarowe wiadomości, jeżeli jest ich za dużo
        if(wiadomosc.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna) await UsunWiadomosciDruzynyPrzekraczajaceLimit(idOdbiorcy); // usuwamy nadmiarowe wiadomości, jeżeli jest ich za dużo
        return await context.SaveChangesAsync() > 0; // zwracamy true, jeżeli dodano więcej niż 0 rekordów, czyli się udało
    }

    // przy usuwaniu znajomości usuwamy też wszystkie wiadomości między tymi użytkownikami
    public async Task<bool> DeleteWiadomosciPrywatneUzytkownikow(int idUzytkownika1, int idUzytkownika2)
    {
        var wiadomosci = await context.Wiadomosc
            .Where(x => (x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna &&
                                   x.IdNadawcy == idUzytkownika1 && x.IdOdbiorcy == idUzytkownika2) ||
                                  (x.IdNadawcy == idUzytkownika2 && x.IdOdbiorcy == idUzytkownika1))
            .ToListAsync();
        context.Wiadomosc.RemoveRange(wiadomosci);
        await context.SaveChangesAsync();
        return true;
    }
    
    private async Task<bool> UsunWiadomosciPrywatnePrzekraczajaceLimit(int idUzytkownika1, int idUzytkownika2)
    {
        
        var wiadomosci = await context.Wiadomosc
            .Where(x => (x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Prywatna &&
                                   x.IdNadawcy == idUzytkownika1 && x.IdOdbiorcy == idUzytkownika2) ||
                                  (x.IdNadawcy == idUzytkownika2 && x.IdOdbiorcy == idUzytkownika1))
            .OrderByDescending(x => x.DataWyslania)
            .ToListAsync();
        if (wiadomosci.Count <= MaksymalnaLiczbaWiadomosciNaCzaciePrywatnym) return true;
        var wiadomosciDoUsuniecia = wiadomosci.Skip(MaksymalnaLiczbaWiadomosciNaCzaciePrywatnym).ToList();
        context.Wiadomosc.RemoveRange(wiadomosciDoUsuniecia);
        await context.SaveChangesAsync();
        return true;
    }
    
    // przy usuwaniu drużyny usuwamy też wszystkie wiadomości na czacie
    public async Task<bool> DeleteWiadomosciDruzyny(int idDruzyny)
    {
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Druzynowa &&x.IdOdbiorcy == idDruzyny)
            .ToListAsync();
        context.Wiadomosc.RemoveRange(wiadomosci);
        await context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> WyczyscNadawceWiadomosciDruzynowych(int idUzytkownika)
    {
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Druzynowa && x.IdNadawcy == idUzytkownika)
            .ToListAsync();
        foreach (var wiadomosc in wiadomosci)
        {
            wiadomosc.IdNadawcy = null;
        }
        await context.SaveChangesAsync();
        return true;
    }
    
    private async Task<bool> UsunWiadomosciDruzynyPrzekraczajaceLimit(int idDruzyny)
    {
        
        var wiadomosci = await context.Wiadomosc
            .Where(x => x.IdTypuWiadomosci == (int)TypWiadomosciEnum.Druzynowa &&x.IdOdbiorcy == idDruzyny)
            .OrderByDescending(x => x.DataWyslania)
            .ToListAsync();
        if (wiadomosci.Count <= MaksymalnaLiczbaWiadomosciNaCzacieDruzynowym) return true;
        var wiadomosciDoUsuniecia = wiadomosci.Skip(MaksymalnaLiczbaWiadomosciNaCzacieDruzynowym).ToList();
        context.Wiadomosc.RemoveRange(wiadomosciDoUsuniecia);
        await context.SaveChangesAsync();
        return true;
    }
}