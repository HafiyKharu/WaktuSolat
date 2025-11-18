export interface ScrapedZone {
  code: string;
  state: string;
  location: string;
}

// Predefined zones data from e-solat.gov.my
// This is more reliable than web scraping which can break when the website changes
const ZONES_DATA: ScrapedZone[] = [
  // Johor
  { code: 'JHR01', state: 'Johor', location: 'Pulau Aur dan Pulau Pemanggil' },
  { code: 'JHR02', state: 'Johor', location: 'Johor Bahru, Kota Tinggi, Mersing, Kulai' },
  { code: 'JHR03', state: 'Johor', location: 'Kluang, Pontian' },
  { code: 'JHR04', state: 'Johor', location: 'Batu Pahat, Muar, Segamat, Gemas Johor, Tangkak' },
  
  // Kedah
  { code: 'KDH01', state: 'Kedah', location: 'Kota Setar, Kubang Pasu, Pokok Sena (Daerah Kecil)' },
  { code: 'KDH02', state: 'Kedah', location: 'Kuala Muda, Yan, Pendang' },
  { code: 'KDH03', state: 'Kedah', location: 'Padang Terap, Sik' },
  { code: 'KDH04', state: 'Kedah', location: 'Baling' },
  { code: 'KDH05', state: 'Kedah', location: 'Bandar Baharu, Kulim' },
  { code: 'KDH06', state: 'Kedah', location: 'Langkawi' },
  { code: 'KDH07', state: 'Kedah', location: 'Puncak Gunung Jerai' },
  
  // Kelantan
  { code: 'KTN01', state: 'Kelantan', location: 'Bachok, Kota Bharu, Machang, Pasir Mas, Pasir Puteh, Tanah Merah, Tumpat, Kuala Krai, Mukim Chiku' },
  { code: 'KTN02', state: 'Kelantan', location: 'Gua Musang (Daerah Galas Dan Bertam), Jeli, Jajahan Kecil Lojing' },
  
  // Melaka
  { code: 'MLK01', state: 'Melaka', location: 'SELURUH NEGERI MELAKA' },
  
  // Negeri Sembilan
  { code: 'NGS01', state: 'Negeri Sembilan', location: 'Tampin, Jempol' },
  { code: 'NGS02', state: 'Negeri Sembilan', location: 'Jelebu, Kuala Pilah, Rembau' },
  { code: 'NGS03', state: 'Negeri Sembilan', location: 'Port Dickson, Seremban' },
  
  // Pahang
  { code: 'PHG01', state: 'Pahang', location: 'Pulau Tioman' },
  { code: 'PHG02', state: 'Pahang', location: 'Kuantan, Pekan, Muadzam Shah' },
  { code: 'PHG03', state: 'Pahang', location: 'Jerantut, Temerloh, Maran, Bera, Chenor, Jengka' },
  { code: 'PHG04', state: 'Pahang', location: 'Bentong, Lipis, Raub' },
  { code: 'PHG05', state: 'Pahang', location: 'Genting Sempah, Janda Baik, Bukit Tinggi' },
  { code: 'PHG06', state: 'Pahang', location: 'Cameron Highlands, Genting Higlands, Bukit Fraser' },
  { code: 'PHG07', state: 'Pahang', location: 'Zon Khas Daerah Rompin, (Mukim Rompin, Mukim Endau, Mukim Pontian)' },
  
  // Perlis
  { code: 'PLS01', state: 'Perlis', location: 'Kangar, Padang Besar, Arau' },
  
  // Pulau Pinang
  { code: 'PNG01', state: 'Pulau Pinang', location: 'Seluruh Negeri Pulau Pinang' },
  
  // Perak
  { code: 'PRK01', state: 'Perak', location: 'Tapah, Slim River, Tanjung Malim' },
  { code: 'PRK02', state: 'Perak', location: 'Kuala Kangsar, Sg. Siput , Ipoh, Batu Gajah, Kampar' },
  { code: 'PRK03', state: 'Perak', location: 'Lenggong, Pengkalan Hulu, Grik' },
  { code: 'PRK04', state: 'Perak', location: 'Temengor, Belum' },
  { code: 'PRK05', state: 'Perak', location: 'Kg Gajah, Teluk Intan, Bagan Datuk, Seri Iskandar, Beruas, Parit, Lumut, Sitiawan, Pulau Pangkor' },
  { code: 'PRK06', state: 'Perak', location: 'Selama, Taiping, Bagan Serai, Parit Buntar' },
  { code: 'PRK07', state: 'Perak', location: 'Bukit Larut' },
  
  // Sabah
  { code: 'SBH01', state: 'Sabah', location: 'Bahagian Sandakan (Timur), Bukit Garam, Semawang, Temanggong, Tambisan, Bandar Sandakan, Sukau' },
  { code: 'SBH02', state: 'Sabah', location: 'Beluran, Telupid, Pinangah, Terusan, Kuamut, Bahagian Sandakan (Barat)' },
  { code: 'SBH03', state: 'Sabah', location: 'Lahad Datu, Silabukan, Kunak, Sahabat, Semporna, Tungku, Bahagian Tawau (Timur)' },
  { code: 'SBH04', state: 'Sabah', location: 'Bandar Tawau, Balong, Merotai, Kalabakan, Bahagian Tawau (Barat)' },
  { code: 'SBH05', state: 'Sabah', location: 'Kudat, Kota Marudu, Pitas, Pulau Banggi, Bahagian Kudat' },
  { code: 'SBH06', state: 'Sabah', location: 'Gunung Kinabalu' },
  { code: 'SBH07', state: 'Sabah', location: 'Kota Kinabalu, Ranau, Kota Belud, Tuaran, Penampang, Papar, Putatan, Bahagian Pantai Barat' },
  { code: 'SBH08', state: 'Sabah', location: 'Pensiangan, Keningau, Tambunan, Nabawan, Bahagian Pendalaman (Atas)' },
  { code: 'SBH09', state: 'Sabah', location: 'Beaufort, Kuala Penyu, Sipitang, Tenom, Long Pasia, Membakut, Weston, Bahagian Pendalaman (Bawah)' },
  
  // Selangor
  { code: 'SGR01', state: 'Selangor', location: 'Gombak, Petaling, Sepang, Hulu Langat, Hulu Selangor, S.Alam' },
  { code: 'SGR02', state: 'Selangor', location: 'Kuala Selangor, Sabak Bernam' },
  { code: 'SGR03', state: 'Selangor', location: 'Klang, Kuala Langat' },
  
  // Sarawak
  { code: 'SWK01', state: 'Sarawak', location: 'Limbang, Lawas, Sundar, Trusan' },
  { code: 'SWK02', state: 'Sarawak', location: 'Miri, Niah, Bekenu, Sibuti, Marudi' },
  { code: 'SWK03', state: 'Sarawak', location: 'Pandan, Belaga, Suai, Tatau, Sebauh, Bintulu' },
  { code: 'SWK04', state: 'Sarawak', location: 'Sibu, Mukah, Dalat, Song, Igan, Oya, Balingian, Kanowit, Kapit' },
  { code: 'SWK05', state: 'Sarawak', location: 'Sarikei, Matu, Julau, Rajang, Daro, Bintangor, Belawai' },
  { code: 'SWK06', state: 'Sarawak', location: 'Lubok Antu, Sri Aman, Roban, Debak, Kabong, Lingga, Engkelili, Betong, Spaoh, Pusa, Saratok' },
  { code: 'SWK07', state: 'Sarawak', location: 'Serian, Simunjan, Samarahan, Sebuyau, Meludam' },
  { code: 'SWK08', state: 'Sarawak', location: 'Kuching, Bau, Lundu, Sematan' },
  { code: 'SWK09', state: 'Sarawak', location: 'Zon Khas (Kampung Patarikan)' },
  
  // Terengganu
  { code: 'TRG01', state: 'Terengganu', location: 'Kuala Terengganu, Marang, Kuala Nerus' },
  { code: 'TRG02', state: 'Terengganu', location: 'Besut, Setiu' },
  { code: 'TRG03', state: 'Terengganu', location: 'Hulu Terengganu' },
  { code: 'TRG04', state: 'Terengganu', location: 'Dungun, Kemaman' },
  
  // Wilayah Persekutuan
  { code: 'WLY01', state: 'Wilayah Persekutuan', location: 'Kuala Lumpur, Putrajaya' },
  { code: 'WLY02', state: 'Wilayah Persekutuan', location: 'Labuan' },
];

export async function scrapeZones(): Promise<ScrapedZone[]> {
  // Return predefined zones data
  // This is more reliable than web scraping
  return ZONES_DATA;
}
