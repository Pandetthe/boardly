export interface ICard {    
    id: string;
    listId: string;
    swimlaneId: string;
    boardId: string;
    title: string;
    color: string;
    description: string | null;
    tags: {id: string, title: string}[];
    assignedUsers: {id: string, nickname:string}[];
    dueDate: string | null;
}
