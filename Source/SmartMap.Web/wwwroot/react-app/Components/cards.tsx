import React, { FunctionComponent, useState, useEffect } from 'react';
import { ICardResponse, ICard } from '../service-model';
import { calculateDistance } from '../Helpers/calculateDistance';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';

export interface ICardState {
    cards: ICard[];
    totalFound: number;
    itemsPerPage: number;
}
const DefaultICardState: ICardState = { cards: [], totalFound: 0, itemsPerPage: 0 };
export const DefaultSortValue: string = 'Random';

export interface ICardRequest {
    cardResponse: ICardResponse;
    currentPage: number;
    positionSetLatLng?: [number, number] | null;
    paginationCallback: (page: number) => void;
    zoomToMarker: (latlng: [number, number]) => void;
    sortingCallback: (sorting: string) => void;
}

export interface ITextTranslation {
    loadMoreCardsText: string;
    loadingText: string;
    digitalCardText: string;
    noCardsFound: string;
    sortTitle: string;
    sortRandom: string;
    sortLatestAdded: string;
    sortLatestUpdated: string;
    sortHeaderAcs: string;
    sortHeaderDesc: string;
    showCardsXofY: string;
}

export const Cards: FunctionComponent<ICardRequest> = (props: ICardRequest) => {
    const [cardState, setCardState] = useState<ICardState>(DefaultICardState);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [sortValue, setSortValue] = useState<string>(DefaultSortValue);
    const [textTranslations, setTextTranslations] = useState<ITextTranslation>();

    useEffect(() => {
        let _service = getService();
        if (_service != null) {
            _service.getTranslations().then(translations => {
                setTextTranslations({
                    loadMoreCardsText: getText('cards.load-more-cards', translations, 'Ladda fler kort'),
                    loadingText: getText('common.loading', translations, 'Laddar...'),
                    digitalCardText: getText('cards.digital-card', translations, 'Digital'),
                    noCardsFound: getText('cards.no-cards-found', translations, 'Inga kort funna'),
                    sortTitle: getText('cards.sort-title', translations, 'Sortera'),
                    sortRandom: getText('cards.sort-random', translations, 'Slumpa'),
                    sortLatestAdded: getText('cards.sort-latest-added', translations, 'Senast tillagd'),
                    sortLatestUpdated: getText('cards.sort-latest-updated', translations, 'Senast uppdaterat'),
                    sortHeaderAcs: getText('cards.sort-header-acs', translations, 'Verksamhet A-Ö'),
                    sortHeaderDesc: getText('cards.sort-header-desc', translations, 'Verksamhet Ö-A'),
                    showCardsXofY: getText('cards.show-x-cards-of-y', translations, 'Visar [ShowingCards] kort av [TotalFound]'),
                });
                
            });
        }
    }, []);

    useEffect(() => {
        let response = props.cardResponse;

        setCardState({
            cards: response.items,
            itemsPerPage: response.itemsPerPage,
            totalFound: response.total
        });

        setIsLoading(response.itemsPerPage <= 0);
    }, [props.cardResponse]);

    const loadMoreCards = () => {
        props.paginationCallback(props.currentPage + 1);
    };

    const positionCard = (e: React.MouseEvent<HTMLSpanElement, MouseEvent>, latlng: [number, number]) => {
        props.zoomToMarker(latlng);
        e.preventDefault();
    };

    const handleSortChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
        setSortValue(event.target.value);
        props.sortingCallback(event.target.value);
    }

    var renderCards: JSX.Element[] = [];
    var renderPagination: JSX.Element = <></>;
    var renderCardsFound: JSX.Element = <></>;
    var renderLoader: JSX.Element = <></>;

    renderCards = cardState.cards.map((c: ICard, i: number) =>
        <div className="col mb-4" key={i}>
            <a className="card-link" href={c.detailPageLink}>
                <div className="card h-100">
                    <div className="position-relative">
                        {c.hasImage ? (
                            <img className="card-img-top mr-2" src={c.imageUrl} alt={c.imageAlt} />
                        ) : (
                            <svg className="bd-placeholder-img card-img-top mr-2" width="100%" xmlns="http://www.w3.org/2000/svg" preserveAspectRatio="xMidYMid slice" focusable="false" role="img" aria-label="Placeholder: Image cap">
                                <title>Placeholder</title>
                                <rect width="100%" height="100%" fill="#c0c5ce"></rect>
                                <text x="50%" y="50%" fill="#dee2e6" dy=".3em">Smarta Kartan</text>
                            </svg>
                        )}
                        <div className="info-over-image">
                            {(props.positionSetLatLng != null && c.addressAndCoordinates?.length === 1) &&
                                <>
                                <span className="badge badge-secondary">
                                    {calculateDistance(
                                        props.positionSetLatLng[0], 
                                        props.positionSetLatLng[1],
                                        c.addressAndCoordinates[0].latitude, 
                                        c.addressAndCoordinates[0].longitude)} km
                                </span>&nbsp;
                                </>
                            }

                            {c.addressAndCoordinates?.length === 1 &&
                                <span onClick={(e) => positionCard(e, [c.addressAndCoordinates[0].latitude, c.addressAndCoordinates[0].longitude])} className="badge badge-primary text-capitalize">
                                    <i className="fas fa-map-marker-alt"></i>
                                </span>
                            }
                        </div>
                    </div>
                    <div className="card-body d-flex align-items-stretch flex-column">
                        {c.onlineOnly ? (
                            <span className="font-weight-light"><i className="fas fa-globe"></i> {textTranslations?.digitalCardText}</span>
                        ) : (
                            c.area == null || c.area.length <= 0 ? (
                                <span className="font-weight-light"><i className="fas fa-map-marker-alt"></i> {c.city}</span>
                            ) : (
                                <span className="font-weight-light"><i className="fas fa-map-marker-alt"></i> {c.area}, {c.city}</span>
                            )
                        )}
                        <h3 className="card-title my-1">{c.header}</h3>
                        <p className="card-text mb-auto">{c.description}</p>
                    </div>
                </div>
            </a>
        </div>
    );

    if ((cardState.itemsPerPage * (props.currentPage + 1)) < cardState.totalFound) {
        renderPagination =
            <button className="btn btn-lg btn-primary btn-block" onClick={loadMoreCards}>
                {textTranslations?.loadMoreCardsText}
            </button>;
    }

    if (isLoading) {
        renderLoader = <div>
            <div className="text-center w-100 pt-5 h4">{textTranslations?.loadingText}</div>
            <div>&nbsp;</div>
        </div>;
    }
    else {
        if (cardState.cards.length <= 0) {
            renderCardsFound = <span className="align-sub cards-found">{textTranslations?.noCardsFound}</span>
        }
        else {
            let showingCards = textTranslations?.showCardsXofY ?? "";
            showingCards = showingCards
                .replace("[ShowingCards]", cardState.cards.length.toString())
                .replace("[TotalFound]", cardState.totalFound.toString());
            renderCardsFound = <span className="align-sub cards-found">{showingCards}</span>
        }
    }

    return <>
        <div className="row">
            <div className="col-6 col-sm-9">{renderCardsFound}</div>
            <div className="col-6 col-sm-3 align-right">
                <div className="form-group">
                    <label className="sr-only" htmlFor="search-sort">{textTranslations?.sortTitle}</label>
                    <select value={sortValue} onChange={handleSortChange} className="form-control form-control-sm" id="search-sort">
                        <option value="Random">{textTranslations?.sortRandom}</option>
                        <option value="LatestAdded">{textTranslations?.sortLatestAdded}</option>
                        <option value="LatestUpdated">{textTranslations?.sortLatestUpdated}</option>
                        <option value="HeaderAcs">{textTranslations?.sortHeaderAcs}</option>
                        <option value="HeaderDesc">{textTranslations?.sortHeaderDesc}</option>
                    </select>
                </div>
            </div>
        </div>

        {renderLoader}

        <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3">
            {renderCards}
        </div>
        <div className="row">
            <div className="col-lg-4 offset-lg-4">
                {renderPagination}
            </div>
        </div>
    </>
}
