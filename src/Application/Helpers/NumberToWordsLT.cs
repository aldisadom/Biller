namespace Application.Helpers;

public static class NumberToWordsLT
{
    private static string OnesSplit(int number)
    {
        string text = number switch
        {
            0 => "nulis",
            1 => "vienas",
            2 => "du",
            3 => "trys",
            4 => "keturi",
            5 => "penki",
            6 => "šeši",
            7 => "septyni",
            8 => "aštuoni",
            9 => "devyni",
            _ => throw new Exception($"OnesSplitLT got {number}"),
        };
        return text;

    }
    private static string TensSplit(int number)
    {
        string text;

        if (number < 10)
            text = OnesSplit(number);
        else if (number >= 20)
        {
            if (number > 99)
                throw new Exception($"TensSplitLT got {number}");

            if (number / 10 == 2)
                text = "tvidešimt";
            else if (number / 10 == 3)
                text = "trisdešimt";
            else
                text = OnesSplit(number / 10) + "asdešimt";

            if (number % 10 != 0)
                text += " " + OnesSplit(number % 10);
        }
        else
        {
            text = number switch
            {
                10 => "dešimt",
                11 => "vienuolika",
                12 => "dvylika",
                13 => "trylika",
                14 => "keturiolika",
                15 => "penkiolika",
                16 => "šešiolika",
                17 => "septyniolika",
                18 => "aštuoniolika",
                19 => "devyniolika",
                _ => throw new Exception($"TensSplitLT got {number}"),
            };
        }

        return text;
    }

    private static string HundredsSplit(int number)
    {
        string text;
        if (number < 100)
            text = TensSplit(number);
        else
        {
            if (number > 999)
                throw new Exception($"HundredsSplitLT got {number}");

            text = OnesSplit(number / 100);
            if (number / 100 == 1)
                text += " šimtas";
            else
                text += " šimtai";

            if (number % 100 != 0)
                text += " " + TensSplit(number % 100);
        }
        return text;
    }

    public static string Decode(decimal price)
    {
        string totalPriceInWords = string.Empty;
        string tmpText = string.Empty;

        totalPriceInWords += $"€ {(int)(price * 100 % 100)} ct.";

        int tmpPrice = (int)(price % 1000);

        if (tmpPrice >= 1)
            tmpText = HundredsSplit(tmpPrice) + " ";

        totalPriceInWords = tmpText + totalPriceInWords;
        price /= 1000;
        tmpPrice = (int)(price % 1000);
        tmpText = string.Empty;

        if ((tmpPrice % 100 > 10 && tmpPrice % 100 < 20) || (tmpPrice > 0 && tmpPrice % 10 == 0))
            tmpText = HundredsSplit(tmpPrice) + " tūkstančių ";
        else if (tmpPrice % 10 == 1)
            tmpText = HundredsSplit(tmpPrice) + " tūkstantis ";
        else if (tmpPrice > 1)
            tmpText = HundredsSplit(tmpPrice) + " tūkstančiai ";

        totalPriceInWords = tmpText + totalPriceInWords;
        price /= 1000;
        tmpPrice = (int)(price % 1000);
        tmpText = string.Empty;

        if ((tmpPrice % 100 > 10 && tmpPrice % 100 < 20) || (tmpPrice > 0 && tmpPrice % 10 == 0))
            tmpText = HundredsSplit(tmpPrice) + " milijonų ";
        else if (tmpPrice % 10 == 1)
            tmpText = HundredsSplit(tmpPrice) + " milijonas ";
        else if (tmpPrice > 1)
            tmpText = HundredsSplit(tmpPrice) + " milijonai ";

        totalPriceInWords = tmpText + totalPriceInWords;

        return totalPriceInWords;
    }
}
