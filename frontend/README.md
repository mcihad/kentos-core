# Kentos Frontend

İstemci uygulamaları ve paylaşılan çekirdek. Backend ile aynı **modüler** yapı: her
backend modülü (`hesap`, `settlement`, …) için tipler ve tipli servis client'ı
`shared` içinde OpenAPI'den **otomatik üretilir** — elle model/servis yazılmaz.

```
frontend/
  shared/    Paylaşılan çekirdek: per-modül üretilen API client'ları (tipler + servis).
             Hem web hem mobile bunu tüketir. (ŞU AN AKTİF GELİŞTİRİLEN TEK YER.)
  webapp/    React (web) uygulaması.            — şimdilik boş, sırada bekliyor.
  mobile/    React Native (Expo) uygulaması.    — şimdilik boş, sırada bekliyor.
```

Şu aşamada yalnızca `shared` üzerinde çalışıyoruz: modüllere göre client üretimi.
`webapp` ve `mobile` başladığında `shared`'ı bağımlılık olarak alır ve buraya bir
pnpm workspace eklenir.

Üretim ve kullanım için bkz. [`shared/README.md`](shared/README.md).
